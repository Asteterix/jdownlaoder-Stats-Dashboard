namespace jdownloader_Stats.Services;

public sealed class SpeedHistory
{
	private readonly long[] _buffer;
	private int _next;
	private int _count;

	public SpeedHistory(int capacity)
	{
		if (capacity <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity));
		}

		_buffer = new long[capacity];
	}

	public int Count => _count;

	public void Add(long value)
	{
		_buffer[_next] = value;
		_next = (_next + 1) % _buffer.Length;
		if (_count < _buffer.Length)
		{
			_count++;
		}
	}

	public IReadOnlyList<long> ToArray()
	{
		var result = new long[_count];
		var start = _count < _buffer.Length ? 0 : _next;
		for (var i = 0; i < _count; i++)
		{
			result[i] = _buffer[(start + i) % _buffer.Length];
		}

		return result;
	}
}
