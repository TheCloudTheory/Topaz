namespace Topaz.Service.Insights.Kql;

internal sealed class ComparableObject : IComparable
{
    private readonly int? _intValue;
    private readonly double? _doubleValue;
    private readonly DateTimeOffset? _dateTimeOffsetValue;
    private readonly object? _value;

    public ComparableObject(object value)
    {
        if (int.TryParse(value.ToString(), out var v))
        {
            _intValue = v;
        }
        else if (double.TryParse(value.ToString(), out var d))
        {
            _doubleValue = d;
        }
        else if (DateTimeOffset.TryParse(value.ToString(), out var dt))
        {
            _dateTimeOffsetValue = dt;
        }
        else
        {
            _value = value;
        }
    }
    
    public int CompareTo(object? obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (int.TryParse(obj.ToString(), out var i))
        {
            if (_intValue > i)
            {
                return 1;
            }

            if (_intValue == i)
            {
                return 0;
            }
            
            if(_intValue < i)
            {
                return -1;
            }
        }
        
        if (double.TryParse(obj.ToString(), out var d))
        {
            if (_doubleValue > d)
            {
                return 1;
            }

            if (_doubleValue.Equals(d))
            {
                return 0;
            }
            
            if(_doubleValue < d)
            {
                return -1;
            }
        }
        
        if (DateTimeOffset.TryParse(obj.ToString(), out var dt))
        {
            if (_dateTimeOffsetValue > dt)
            {
                return 1;
            }

            if (_dateTimeOffsetValue.Equals(dt))
            {
                return 0;
            }
            
            if(_dateTimeOffsetValue < dt)
            {
                return -1;
            }
        }

        if (_value == null)
        {
            throw new InvalidOperationException();
        }
        
        // If the value is none of the checked types, fallback to comparing strings
        return string.Compare(_value.ToString()!, obj.ToString(), StringComparison.Ordinal);
    }
}

internal static class ComparableObjectExtensions
{
    public static bool IsGreaterThan(this ComparableObject obj, object other)
    {
        var result = obj.CompareTo(other);
        return result > 0;
    }
    
    public static bool IsEqualTo(this ComparableObject obj, object other)
    {
        var result = obj.CompareTo(other);
        return result == 0;
    }
    
    public static bool IsLessThan(this ComparableObject obj, object other)
    {
        var result = obj.CompareTo(other);
        return result < 0;
    }
    
    public static ComparableObject AsComparableObject(this object obj)
    {
        return new ComparableObject(obj);
    }
}