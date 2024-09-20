using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CaiBotMod.Common;

[Serializable]
public class RestObject : Dictionary<string, object>
{
    public string Status
    {
        get
        {
            return this["status"] as string;
        }
        set
        {
            this["status"] = value;
        }
    }

    public string Error
    {
        get
        {
            return this["error"] as string;
        }
        set
        {
            this["error"] = value;
        }
    }

    public string Response
    {
        get
        {
            return this["response"] as string;
        }
        set
        {
            this["response"] = value;
        }
    }

    //
    // 摘要:
    //     Gets value safely, if it does not exist, return null. Sets/Adds value safely,
    //     if null it will remove.
    //
    // 参数:
    //   key:
    //     the key
    //
    // 返回结果:
    //     Returns null if key does not exist.
    public new object this[string key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }
        set
        {
            if (!ContainsKey(key))
            {
                if (value != null)
                {
                    Add(key, value);
                }
            }
            else if (value != null)
            {
                base[key] = value;
            }
            else
            {
                Remove(key);
            }
        }
    }

    public RestObject()
    {
        Status = "200";
    }

    public RestObject(string status = "200")
    {
        Status = status;
    }

    internal string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}