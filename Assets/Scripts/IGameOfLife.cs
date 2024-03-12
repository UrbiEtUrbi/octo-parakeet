using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameOfLife
{
    public void Process(ref HashSet<(int x, int y)> Cells);
    public void Process(ref bool[] Cells);
    public void Init(MonoBehaviour parent);
}
