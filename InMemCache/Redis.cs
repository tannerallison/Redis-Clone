using System.Text.RegularExpressions;

namespace InMemCache;

public interface RESPType
{
    public string Serialize();
}

public class RESPString : RESPType
{
    public string Value { get; set; }

    public string Serialize() => $"+{Value}\r\n";
}

public class RESPInteger : RESPType
{
    public int Value { get; set; }

    public string Serialize() => $":{Value}\r\n";
}

public class RESPBulkString : RESPType
{
    public string Value { get; set; }

    public string Serialize() => $":{System.Text.ASCIIEncoding.ASCII.GetByteCount(Value)}\r\n:{Value}\r\n";

    public static RESPType Deserialize(string s)
    {
        if (s.Length < 3)
            throw new ArgumentException();
        var matches = Regex.Matches(s, "\\$(\\-?\\d+)\\r\\n(.*)(?:\\r\\n)?");
        if (matches.Count < 1)
            throw new ArgumentException();

        var val = int.Parse(s.Skip(1).SkipLast(2).ToString()!);
        return new RESPInteger { Value = val };
    }
}

public class RESPError : RESPType
{
    public string Value { get; set; }

    public string Serialize() => $"-{Value}\r\n";
    public static RESPType Deserialize(string s) => throw new NotImplementedException();
}

public class RESPArray : RESPType
{
    public List<RESPType> Value { get; set; }

    public string Serialize() => $"*{Value.Count}\r\n{string.Join("", Value.Select(v => v.Serialize()))}";

    public static RESPType Deserialize(string s) => throw new NotImplementedException();
}

public class Redis
{

    public string ParseInput(string input)
    {
        switch (input)
        {
            case "PING":
                return "PONG";
            case "ECHO":
            default:
                return "Unable to parse";
        }
    }


    public RESPType? Deserialize(string s)
    {
        return Deserialize(s, out _);
    }

    public RESPType? Deserialize(string s, out string remainder)
    {
        if (s.Length < 1)
            throw new ArgumentException("string not long enough");
        var splitIndex = s.IndexOf('\r');
        switch (s[0])
        {
            case '+':
                if (s.Length < 3)
                    throw new ArgumentException();
                var stringVal = s.Substring(1, splitIndex - 1);
                remainder = s.Substring(splitIndex + 2);
                return new RESPString { Value = stringVal };
            case ':':
                if (s.Length < 3)
                    throw new ArgumentException();
                var intVal = int.Parse(s.Substring(1, splitIndex - 1));
                remainder = s.Substring(splitIndex + 2);
                return new RESPInteger { Value = intVal };
            case '-':
                if (s.Length < 3)
                    throw new ArgumentException();
                var errVal = s.Substring(1, splitIndex - 1);
                remainder = s.Substring(splitIndex + 2);
                return new RESPError() { Value = errVal };
            case '$':
                if (s.Length < 3)
                    throw new ArgumentException();
                var byteCount = int.Parse(s.Substring(1, splitIndex - 1));
                if (byteCount == -1)
                {
                    remainder = s.Substring(splitIndex + 2);
                    return new RESPBulkString { Value = null };
                }
                var bulkStringVal = s.Substring(splitIndex + 2, byteCount);
                remainder = s.Substring(splitIndex + 2 + byteCount + 2);
                return new RESPBulkString { Value = bulkStringVal };
            case '*':
                if (s.Length < 3)
                    throw new ArgumentException();
                var arrSplit = s.IndexOf('\r');
                var arrCount = int.Parse(s.Substring(1, arrSplit - 1));
                var arrString = s.Substring(arrSplit + 2);
                RESPArray arr = new RESPArray { Value = new List<RESPType>() };
                for (int i = 0; i < arrCount; i++)
                {
                    var subItem = Deserialize(arrString, out arrString);
                    arr.Value.Add(subItem);
                }

                remainder = arrString;
                return arr;
            default:
                remainder = s.Substring(s.IndexOf('\r') + 2);
                return null;
        }
    }
}
