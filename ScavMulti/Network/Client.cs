using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Buffers.Binary;
using MessagePack;
using ScavMulti.Network.Messages;

namespace ScavMulti.Network;

public partial class Client : IDisposable
{
	public Socket Sock { get; private set; }
	private readonly ConcurrentQueue<(byte[] array, int length, bool isPooled)> _inputQueue;
	private readonly ConcurrentQueue<MessageBase> _outputQueue;
	private readonly MemoryStream _sendIntermediateStream;
	private readonly Sender _sender;
	private readonly Receiver _receiver;
	private readonly SemaphoreSlim _inputQueueEnqueuedEvent;
	private readonly ClientCancellationContext _cancellationContext;
	public ClientState State => _cancellationContext.State;
	public bool IsRunning => _cancellationContext.State == ClientState.Running;
	public ClientCancelledException ClientCancelledException => _cancellationContext.ClientCancelledException;

	public Client(Socket sock)
	{
		Sock = sock;
		_inputQueue = new();
		_outputQueue = new();
		_sendIntermediateStream = new(4096);
		_receiver = new(sock);
		_sender = new(sock);
		_inputQueueEnqueuedEvent = new(0, int.MaxValue);
		_cancellationContext = new();
	}

	void AssertIsRunning()
	{
		if (_cancellationContext.State == ClientState.NotRunningYet)
			throw new InvalidOperationException("Client is not running");
		else if (_cancellationContext.State != ClientState.Running)
			throw ClientCancelledException;
	}

	void AssertIsNotRunning()
	{
		if (IsRunning)
			throw new InvalidOperationException("Client is already running");
	}

	public void Enqueue(MessageBase data)
	{
		AssertIsRunning();
		_sendIntermediateStream.Position = sizeof(int);
		MessagePackSerializer.Serialize<MessageBase>(_sendIntermediateStream, data);
		int length = (int)_sendIntermediateStream.Position;
		BinaryPrimitives.WriteInt32LittleEndian(_sendIntermediateStream.GetBuffer(), length - sizeof(int));
		var array = ArrayPool<byte>.Shared.Rent(length);
		unsafe
		{
			fixed (byte* dest = array, src = _sendIntermediateStream.GetBuffer())
			{
				Buffer.MemoryCopy(src, dest, length * sizeof(byte), length * sizeof(byte));
			}
		}
		_inputQueue.Enqueue((array, length, true));
		_inputQueueEnqueuedEvent.Release();
	}

	public void Enqueue(byte[] array, int length, bool isArrayRented)
	{
		AssertIsRunning();
		_inputQueue.Enqueue((array, length, isArrayRented));
		_inputQueueEnqueuedEvent.Release();
	}

	public bool TryDequeue(out MessageBase data)
	{
		AssertIsRunning();
		return _outputQueue.TryDequeue(out data);
	}

	public MessageBase Dequeue()
	{
		if (!TryDequeue(out MessageBase data))
			throw new InvalidOperationException("Queue empty");
		return data;
	}

	public T Dequeue<T>() where T : MessageBase
	{
		if (!TryDequeue(out MessageBase data))
			throw new InvalidOperationException("Queue empty");
		if (data.GetType() == typeof(T))
			return (T)data;
		throw new InvalidOperationException($"Expected network object of type {typeof(T)} but got {data.GetType()}");
	}

	public bool IsEmpty
	{
		get
		{
			AssertIsRunning();
			return _outputQueue.IsEmpty;
		}
	}

	public IEnumerator WaitUntilHasData()
	{
		AssertIsRunning();
		while (_outputQueue.IsEmpty)
		{
			yield return null;
			AssertIsRunning();
		}
	}

	private async Task ReceiveLoop()
	{
		var lengthBytes = new byte[sizeof(int)];
		var bytes = new byte[4096];
		try
		{
			int length;
			MessageBase data;
			while (true)
			{
				if (!await ReceiveFullAsync(lengthBytes, sizeof(int)))
				{
					_cancellationContext.CancelFromRecv(null);
					break;
				}
				if  (_cancellationContext.Token.IsCancellationRequested)
					break;
				length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
				if (length > bytes.Length)
					Array.Resize(ref bytes, length);
				if (!await ReceiveFullAsync(bytes, length))
				{
					_cancellationContext.CancelFromRecv(null);
					break;
				}
				if (_cancellationContext.Token.IsCancellationRequested)
					break;
				data = MessagePackSerializer.Deserialize<MessageBase>(bytes);
				if (data == null)
					throw new NullReferenceException("Received data is null");
				_outputQueue.Enqueue(data);
			}
		}
		catch (Exception e)
		{
			_cancellationContext.CancelFromRecv(e);
		}
	}

	private async Task SendLoop()
	{
		try
		{
			(byte[] array, int length, bool isPooled) toSend;
			bool clientDisconnected;
			while (!_cancellationContext.Token.IsCancellationRequested)
			{
				while (!_inputQueue.TryDequeue(out toSend))
					await _inputQueueEnqueuedEvent.WaitAsync(_cancellationContext.Token);
				clientDisconnected = !await SendFullAsync(toSend.array, toSend.length);
				if (toSend.isPooled)
					ArrayPool<byte>.Shared.Return(toSend.array);
				if (clientDisconnected)
				{
					_cancellationContext.CancelFromSend(null);
					break;
				}
			}
		}
		catch (Exception e)
		{
			_cancellationContext.CancelFromSend(e);
		}
	}

	private async Task<bool> SendFullAsync(byte[] buffer, int count)
	{
		int sentSoFar = 0;
		int sent;
		do
		{
			sent = await _sender.SendAsync(buffer, sentSoFar, count - sentSoFar);
			if (sent == 0)
				return false;
			sentSoFar += sent;
		} while (sentSoFar != count);
		return true;
	}

	private async Task<bool> ReceiveFullAsync(byte[] buffer, int count)
	{
		int receivedSoFar = 0;
		int received;
		do
		{
			received = await _receiver.ReceiveAsync(buffer, receivedSoFar, count - receivedSoFar);
			if (received == 0)
				return false;
			receivedSoFar += received;
		} while (receivedSoFar != count);
		return true;
	}

	public Task Start()
	{
		AssertIsNotRunning();
		_cancellationContext.SetIsRunning();
		return Task.Run(_Start);
	}

	private async Task _Start()
	{
		var sendTask = SendLoop();
		var recvTask = ReceiveLoop();
		await Task.WhenAll(sendTask, recvTask);
	}

	public void Stop()
	{
		_cancellationContext.Cancel();
	}

	private bool _disposed = false;
	public void Dispose()
	{
		if (_disposed)
			return;
		_sendIntermediateStream.Dispose();
		_cancellationContext.Dispose();
		_inputQueueEnqueuedEvent.Dispose();
		Sock?.Dispose();
		_disposed = true;
	}
}
