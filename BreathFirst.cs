using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class BreathFirst : MonoBehaviour
{
    public Texture2D inputTexture;
    //Midas
    public ModelAsset modelAsset;
    public Material Debugmat;
    MidasEstimation midas;

    public Material labelDebugmat;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        midas = new MidasEstimation(modelAsset, Debugmat);
        RenderTexture outputtex = midas.inference(inputTexture);
        Color[] pixels= RenderTexture2Color(outputtex);
        LabelBreadthFirstSerch(pixels,outputtex.width,outputtex.height, labelDebugmat);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        midas.Release();
    }
    Color[] RenderTexture2Color(RenderTexture renderTexture)
    {

        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null; // 忘れずに解除

        // 任意のピクセル値を取得
        return texture2D.GetPixels();
    }
    (int [] ,int) LabelBreadthFirstSerch(Color[] InputTex, int width, int height,Material Debugmat)
    {
        int[] labels = new int[width * height];
        
        int componentCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (labels[x + y * width] == 0)
                {
                    componentCount++;
                    BFS(x, y, componentCount, InputTex, labels, width, height);
                }
            }
        }
        Color[] distinctColors = GenerateDistinctColors(componentCount+1);


        Debug.Log("componentCount: "+ componentCount);
        Texture2D texture = new Texture2D(width, height);
        for (int i = 0; i < width; i++)
        {
            for(int j=0;j< height; j++)
            {
                int idx = i + j * width;

                texture.SetPixel(i,j, distinctColors[labels[idx]]);
                
            }
        }
        
        
        texture.Apply();
        Debugmat.mainTexture = texture;
        return (labels,componentCount);
    }
    public Color[] GenerateDistinctColors(int count)
    {
        Color[] colors = new Color[count];
        float hueStep = 1.0f / count; // 色相を均等に分割

        for (int i = 0; i < count; i++)
        {
            float hue = i * hueStep; // 色相
            float saturation = 0.8f; // 彩度（高めに設定）
            float value = 0.9f; // 明度（明るめに設定）

            // HSVからRGBカラーを生成
            Color color = Color.HSVToRGB(hue, saturation, value);
            colors[i]=color;
        }

        return colors;
    }



    void BFS(int startX, int startY, int label, Color[] pixels,int[] labels, int width, int height)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        labels[startX + startY * width] = label;
        
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int x = current.x;
            int y = current.y;
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && labels[nx + ny * width] == 0
                    && Math.Abs(pixels[nx + ny * width].r - pixels[x+y*width].r)<0.01f)
                {
                    queue.Enqueue(new Vector2Int(nx, ny));
                    labels[nx + ny * width] = label;
                 
                }
            }
        }
      
    }
}
