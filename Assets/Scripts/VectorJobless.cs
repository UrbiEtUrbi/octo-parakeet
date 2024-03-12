using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorJobless : IGameOfLife
{
    public void Init(MonoBehaviour parent)
    {

    }



    public void Process(ref HashSet<(int x, int y)> Cells)
    {
        HashSet<(int x, int y)> ToCheck = new();

        HashSet<(int x, int y)> ToRemove = new();
        HashSet<(int x, int y)> ToAdd = new();


        foreach (var c in Cells)
        {
            int neighbours = 0;
            foreach (var n in GameController.NeighbourIndicesArray)
            {

                var neigh = (c.x + n.x, c.y + n.y);
                if (Cells.Contains(neigh))
                {
                    neighbours++;
                }
                else
                {
                    ToCheck.Add(neigh);
                }

                if (neighbours == 4)
                {
                    ToRemove.Add(c);
                }
            }
            if (neighbours <= 1)
            {
                ToRemove.Add(c);
            }

        }



        foreach (var empty in ToCheck)
        {
            int neighbo = 0;
            foreach (var n in GameController.NeighbourIndicesArray)
            {
                if (Cells.Contains((empty.x + n.x, empty.y + n.y)))
                {
                    neighbo++;
                }

                if (neighbo > 3)
                {
                    break;
                }
            }
            if (neighbo == 3)
            {
                ToAdd.Add(empty);
            }

        }


        var offsetX = Tex.width / 2;

        var offsetY = Tex.height / 2;
        foreach (var ta in ToAdd)
        {
            //if (ta.x > -offsetX && ta.x < offsetX && ta.y > -offsetY && ta.y < offsetY)
            //{
            //    Tex.SetPixel(offsetX + ta.x, offsetY + ta.y, Color.white);

            //}
            Cells.Add(ta);
        }

        foreach (var tr in ToRemove)
        {
            //if (tr.x > -offsetX && tr.x < offsetX && tr.y > -offsetY && tr.y < offsetY)
            //{
            //    Tex.SetPixel(offsetX + tr.x, offsetY + tr.y, Color.black);

            //}
            Cells.Remove(tr);
        }

    }

    public void Process(ref bool[] cells)
    {
        throw new System.NotImplementedException();
    }
}
