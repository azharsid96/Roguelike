using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinPriorityQueue<T,U> where U : System.IComparable
{
    private List<T> objects = new List<T>();
    private List<U> priorities = new List<U>();

    public int Count
    {
        get
        {
            return objects.Count;
        }
    }

    public bool Contains(T t)
    {
        return objects.Contains(t);
    }

    public void Add(T t, U priority)
    {
        int index = objects.IndexOf(t);
        // tile t was not found in the queue
        if (index < 0)
        {
            objects.Add(t);
            priorities.Add(priority);
        }
        // already found tile t in the queue
        else
        {
            priorities[index] = priority;
        }
    }

    public U GetValue(T t)
    {
        int index = objects.IndexOf(t);
        return priorities[index];
    }

    public T Pop(out U u)
    {
        U minPriority = default(U);
        T minKey = default(T);
        int minIndex = -1;

        for (int i = 0; i < objects.Count; i++)
        {
            U priority = priorities[i];
            if (minIndex < 0 || (priority.CompareTo(minPriority) < 0))
            {
                minPriority = priority;
                minKey = objects[i];
                minIndex = i;
            }
        }

        u = minPriority;
        //T t = objects[0];

        objects.RemoveAt(minIndex);
        priorities.RemoveAt(minIndex);

        return minKey;
    }
}
