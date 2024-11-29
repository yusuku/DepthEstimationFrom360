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
    // Start is called before the first frame update
    void Start()
    {
        tensor = TextureConverter.ToTensor(inputtex, width: 1024, height: 512);
        
        var model = ModelLoader.Load(modelAsset);
        m_Worker = new Worker(model, BackendType.GPUCompute);
        m_Worker.Schedule(tensor);
        var outputTensor = m_Worker.PeekOutput("output") as Tensor<float>;
        
        Texture2D tex=TensorToTexture2D(outputTensor.ReadbackAndClone());
        mat.mainTexture = tex;
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
