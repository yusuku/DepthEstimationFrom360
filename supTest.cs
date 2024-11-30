using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Assertions;

public class supTest : MonoBehaviour
{
    public ModelAsset modelAsset;
    public Texture2D inputtex;
    Worker m_Worker;
    public RenderTexture outputtex;
    Tensor<float> tensor;
    public Material mat;


    void Start()
    {
        
        mat.mainTexture = model2Text(modelAsset);

    }
    Texture2D model2Text(ModelAsset modelAsset)
    {
        tensor = TextureConverter.ToTensor(inputtex, width: 1024, height: 512);

        var model = ModelLoader.Load(modelAsset);
        m_Worker = new Worker(model, BackendType.GPUCompute);
        m_Worker.Schedule(tensor);
        var outputTensor = m_Worker.PeekOutput("output") as Tensor<float>;

        Texture2D tex = TensorToTexture2D(outputTensor.ReadbackAndClone());
        tex = CropTexture2D(tex, 70, 450);
        return tex;
    }
    Texture2D CropTexture2D(Texture2D original, int top, int bottom)
    {
        int width = original.width;
        int croppedHeight = bottom - top;

        // 新しいテクスチャを作成
        Texture2D croppedTexture = new Texture2D(width, croppedHeight, original.format, false);

        // クロップされた範囲のピクセルを取得
        Color[] pixels = original.GetPixels(0, top, width, croppedHeight);
        croppedTexture.SetPixels(pixels);

        croppedTexture.Apply();
        return croppedTexture;
    }
    Texture2D TensorToTexture2D(Tensor<float> tensor)
    {
        int width = tensor.shape[2];
        int height = tensor.shape[1];

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = tensor[0, y, x];  // 値を取得

                if (min > value) min = value;
                if (max < value) max = value;
            }
        }
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = tensor[0, y, x];  // 値を取得
                value = (value - min) / (max - min);
                Color color = new Color(value, value, value, 1.0f);  // グレースケールに変換
                texture.SetPixel(x, y, color);

            }
        }

        texture.Apply();
        return texture;
    }
    // Update is called once per frame
    void Update()
    {
    }
       
    void OnDisable()
    {
        // Clean up Sentis resources.
        m_Worker.Dispose();
        tensor.Dispose();
       

    }
    
}
