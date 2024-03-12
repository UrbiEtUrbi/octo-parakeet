using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Jobs;

public class GameController : MonoBehaviour
{

   public HashSet<(int x, int y)> Cells;
    public NativeHashSet<(int x, int y)> CellsNative = new();

    public NativeArray<(int x, int y)> CellsNativeArray = new();

    public NativeHashSet<(int x, int y)> ToAddNative;
    public NativeArray<(int x, int y)> ToRemoveNative;

    public NativeParallelHashSet<(int x, int y)> ToCheckNative;



    public static NativeArray<(int x, int y)> NeighbourIndicesNativeArray;
    public static (int x, int y)[] NeighbourIndicesArray;


    Texture2D Tex;

    Color[] Colors;

    [SerializeField]
    RawImage Image;


    [SerializeField, TextArea]
    string StartField;
    [SerializeField]
   Vector2Int StartOffset;

    [SerializeField]
    string Alive, Dead;


    [SerializeField]
    int TextureSize;

    [SerializeField]
    bool RLE;


    public ProcessType Method;


    Stopwatch Stopwatch;

    private void Start()
    {
        Stopwatch = new();
        NeighbourIndicesNativeArray = GetNeighboursNative();
        NeighbourIndicesArray = GetNeighbours();
        Tex = new Texture2D(TextureSize, TextureSize, UnityEngine.Experimental.Rendering.DefaultFormat.LDR,flags: UnityEngine.Experimental.Rendering.TextureCreationFlags.IgnoreMipmapLimit);
        Colors = new Color[Tex.width * Tex.height];
        System.Array.Fill(Colors, Color.white);
      
        Tex.filterMode = FilterMode.Point;
        Image.texture = Tex;
        
        Cells = new();

        

        //for (int i = -Tex.width/2; i < Tex.width/2; i++)
        //{

        //    for (int j = -Tex.width/2; j < Tex.width/2; j++)
        //    {
        //        if (Random.value > 0.8f)
        //        {
        //            Cells.Add((i, j));
        //        }
        //    }

        //}
        ParseStart();

        UpdateTexture();

    }



    float Timer = 0;
    float time = 0f;
    private void Update()
    {
        if (Input.GetKey(KeyCode.Space) && Timer > time)
        {

            Timer = 0;
            //Cells = new();



            //for (int i = -Tex.width / 2; i < Tex.width / 2; i++)
            //{

            //    for (int j = -Tex.width / 2; j < Tex.width / 2; j++)
            //    {
            //       // if (i % 2 == 0 && Mathf.Abs(j % 2) == 1)
            //        //{

            //            Cells.Add((i, j));
            //        //}
            //    }

            //}
            Process();
        }

        Timer += Time.deltaTime;
      
    }


    NativeArray<(int, int)> GetNeighboursNative()
    {
        var tmp = new NativeArray<(int x, int y)>(8, Allocator.Persistent);
        int count = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (i == 0 && j == 0)
                {
                    continue;
                }
                tmp[count] = (i, j);
                count++;
            }
        }
        return tmp;
    }

    (int, int)[] GetNeighbours()
    {
        var tmp = new (int, int)[8];
        int count = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (i == 0 && j == 0)
                {
                    continue;
                }
                tmp[count] = (i, j);
            }
        }
        return tmp;
    }

    JobHandle jobHandle;


    IEnumerator WaitForFrame()
    {
        yield return new  WaitForEndOfFrame();
        jobHandle.Complete();


        HashSet<(int x, int y)> ToAdd = new();
        foreach (var empty in ToCheckNative)
        {
            if(empty == (0,0)){
                continue;
            }
            int neighbo = 0;
            foreach (var n in NeighbourIndicesArray)
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
            if (ta.x > -offsetX && ta.x < offsetX && ta.y > -offsetY && ta.y < offsetY)
            {
                Tex.SetPixel(offsetX + ta.x, offsetY + ta.y, Color.white);

            }
            Cells.Add(ta);
        }

        
        foreach (var tr in ToRemoveNative)
        {
            if (tr == (0, 0))
            {
                continue;
            }
            if (tr.x > -offsetX && tr.x < offsetX && tr.y > -offsetY && tr.y < offsetY)
            {
                Tex.SetPixel(offsetX + tr.x, offsetY + tr.y, Color.black);

            }
            Cells.Remove(tr);
        }
        Tex.Apply();
      //  CellsNative.Dispose();
       // ToAddNative.Dispose();
        ToRemoveNative.Dispose();
        CellsNativeArray.Dispose();

    }
    public void Process()
    {

        var jobArray = new CheckAlive();

        ToCheckNative = new NativeParallelHashSet<(int x, int y)>(Cells.Count*8, Allocator.TempJob);
        ToRemoveNative = new NativeArray<(int x, int y)>(Cells.Count, Allocator.TempJob);
        CellsNativeArray = new NativeArray<(int x, int y)>(Cells.Count, Allocator.TempJob);
        int i = 0;
        foreach(var cell in Cells)
        {

            CellsNativeArray[i] = cell;
            i++;
        }

        jobArray.countCheck = 0;
        jobArray.removeCheck = 0;
        jobArray.NeighbourIndices = NeighbourIndicesNativeArray;
        jobArray.ToCheck = ToCheckNative.AsParallelWriter();
        jobArray.Cells = CellsNativeArray;
        jobArray.ToRemove = ToRemoveNative;
        jobHandle = jobArray.Schedule(Cells.Count, 16);

        StartCoroutine(WaitForFrame());
        return;

        //var job = new Calculate();

        //job.NeighbourIndices = NeighbourIndices;
        //CellsNative = new NativeHashSet<(int x, int y)>(Cells.Count, Allocator.TempJob);

        //ToAddNative = new NativeHashSet<(int x, int y)>(Cells.Count, Allocator.TempJob);
        //ToRemoveNative = new NativeHashSet<(int x, int y)>(Cells.Count, Allocator.TempJob);
        //foreach (var c in Cells)
        //{
        //    CellsNative.Add(c);
        //}
        //job.Cells = CellsNative;
        //job.ToAdd = ToAddNative;
        //job.ToRemove = ToRemoveNative;
        //jobHandle = job.Schedule();

        //StartCoroutine(WaitForFrame());

        return;
       

    }

    void UpdateTexture()
    {

        Stopwatch.Start();
        var offsetX = Tex.width / 2;

        var offsetY = Tex.height / 2;

        for (int i = 0; i < Colors.Length; i++)
        {
            Colors[i] = Color.black;

        }

        foreach (var c in Cells)
        {
            if (c.x > -offsetX && c.x < offsetX  && c.y > -offsetY && c.y < offsetY)
            {
                Colors[offsetX + c.x + (offsetY + c.y) * Tex.width] = Color.white;
            }
        }

        Tex.SetPixels(Colors);
        Tex.Apply();
        Stopwatch.Stop();
        Debug.Log($"texture {Stopwatch.Elapsed}");
    }



    void ParseStart()
    {
        int i = 0;
        int j = 0;


        if (RLE)
        {

            int lastIndex = 0;
            int currentIndex = 0;
            foreach (var c in StartField)
            {
                if (!char.IsNumber(c))
                {
                    if (c == 'b')
                    {
                        var str = StartField.Substring(lastIndex, currentIndex - lastIndex);
                        if (string.IsNullOrWhiteSpace(str))
                        {
                            str = "1";
                        }
                        int n = int.Parse(str);
                        i += n;
                        lastIndex = currentIndex+1;
                    }
                    else if (c == 'o')
                    {
                        var str = StartField.Substring(lastIndex, currentIndex - lastIndex);

                        if (string.IsNullOrWhiteSpace(str))
                        {
                            str = "1";
                        }
                        int n = int.Parse(str);
                        for (int iii = 0; iii < n; iii++)
                        {
                            Cells.Add((iii +i+ StartOffset.x, j + StartOffset.y));
                        }
                        i += n;
                        lastIndex = currentIndex+1;
                    }
                    else if (c == '$')
                    {
                        j--;
                        i = 0;
                        lastIndex = currentIndex+1;
                    }
                    else if (c == '!'){
                        break;
                    }
                }

                currentIndex++;
                
            }
        }
        else
        {


            foreach (var c in StartField)
            {
                if (c.ToString() == Alive)
                {
                    Cells.Add((i + StartOffset.x, j + StartOffset.y));
                }
                i++;

                if (c == '\n')
                {
                    j--;
                    i = 0;
                }



            }

        }

    }


}


public struct CheckAlive : IJobParallelFor
{

    public NativeArray<(int x, int y)> Cells;
    public NativeArray<(int x, int y)> NeighbourIndices;
    public NativeParallelHashSet<(int x, int y)>.ParallelWriter ToCheck;
    public NativeArray<(int x, int y)> ToRemove;

    public int countCheck;
    public int removeCheck;
    public void Execute(int index)
    {
        int neighbours = 0;
        var c = Cells[index];
        foreach (var n in NeighbourIndices)
        {

            var neigh = (c.x + n.x, c.y + n.y);
            if (Cells.Contains(neigh))
            {
                neighbours++;
            }
            else
            {
                ToCheck.Add(neigh);
                //if (!ToCheck.Contains(neigh))
                //{
                    //ToCheck[countCheck] = neigh;
                    //countCheck++;
                //}
            }

            if (neighbours == 4)
            {
                if (!ToRemove.Contains(c))
                {
                    ToRemove[index] = c;
                    removeCheck++;
                }
            }
        }
        if (neighbours <= 1)
        {
            if (!ToRemove.Contains(c))
            {
                ToRemove[index] = c;
                removeCheck++;
            }
        }
    }
}

public struct CheckDead : IJobParallelFor
{

    public NativeArray<(int x, int y)> Cells;
    public NativeArray<(int x, int y)> NeighbourIndices;
    public NativeArray<(int x, int y)> ToCheck;
    public NativeHashSet<(int x, int y)> ToAdd;
    public void Execute(int index)
    {
        var empty = ToCheck[index];
            int neighbo = 0;
            foreach (var n in NeighbourIndices)
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
}

public struct Calculate : IJob
{
    public NativeHashSet<(int x, int y)> Cells;
    public NativeArray<(int x, int y)> NeighbourIndices;

    public NativeHashSet<(int x, int y)> ToAdd;

    public NativeHashSet<(int x, int y)> ToRemove;




    public void Execute()
    {
        HashSet<(int x, int y)> ToCheck = new();


        foreach (var c in Cells)
        {
            int neighbours = 0;
            foreach (var n in NeighbourIndices)
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
            foreach (var n in NeighbourIndices)
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
      
       

    }
}

public enum ProcessType
{
    Vector,
    Array,
    JobVector,
    JobParallel1,
    JobParallel2



}




