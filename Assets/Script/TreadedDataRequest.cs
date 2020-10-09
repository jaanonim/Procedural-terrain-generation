using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class TreadedDataRequest : MonoBehaviour
{
    static TreadedDataRequest instance;
    display display;

    Queue<TredInfo> DataTreadInfoQ = new Queue<TredInfo>();

    private void Awake()
    {
        instance = FindObjectOfType<TreadedDataRequest>() as TreadedDataRequest;
        display = FindObjectOfType<display>() as display;
    }

    public void Update()
    {
        if(display.IsEndlessMode)
        {
            if (DataTreadInfoQ.Count > 0)
            {
                for (int i = 0; i < DataTreadInfoQ.Count; i++)
                {
                    TredInfo tredInfo = DataTreadInfoQ.Dequeue();
                    tredInfo.calback(tredInfo.prameter);
                }
            }
        }
        
    }

    public static void RequestData(Func<object> genData, Action<object> callback)
    {
        ThreadStart tStart = delegate
        {
            instance.ThreadData(genData, callback);
        };

        new Thread(tStart).Start();
    }

    public void ThreadData(Func<object> genData, Action<object> callback)
    {
        object data = genData();
        lock (DataTreadInfoQ)
        {
            DataTreadInfoQ.Enqueue(new TredInfo(callback, data));
        }
    }

    struct TredInfo
    {
        public readonly Action<object> calback;
        public readonly object prameter;

        public TredInfo(Action<object> calback, object prameter)
        {
            this.calback = calback;
            this.prameter = prameter;
        }
    }

}

