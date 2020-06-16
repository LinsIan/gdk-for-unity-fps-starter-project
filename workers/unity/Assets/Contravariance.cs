using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;




abstract public class Fruit : IComparable<Fruit>
{
    public float Price { get; set; }
    public int CompareTo(Fruit fruit)
    {
        fruit = new Apple();
        return Price.CompareTo(fruit.Price);
    }
}

public class Apple : Fruit
{
}

public class Banana : Fruit
{
}


public class Contravariance : MonoBehaviour
{
    private Apple apple = new Apple();
    private Banana banana = new Banana();
    

    public void Main()
    {
        Apple apple = new Apple() { Price = 10 };
        Banana banana = new Banana() { Price = 5 };
        Fruit fruit = apple;

    }

}


/*
abstract public class Fruit
{
    public int Price { get; set; }
}

public class Apple : Fruit
{
}

public class FruitComparer : IComparer<Fruit>
{
    public int Compare(Fruit x, Fruit y)
    {
        return x.Price - y.Price;
    }
}


public class Contravariance
{
    public void Main()
    {
        List<Apple> apples = new List<Apple>();
        apples.Add(new Apple() { Price = 1 });
        apples.Add(new Apple() { Price = 2 });
        apples.Add(new Apple() { Price = 3 });

        IComparer<Fruit> comparer = new FruitComparer();
        apples.Sort(comparer);//這邊參數是(IComparer<Apple>())，反變數可以父轉子

    }

}
*/
