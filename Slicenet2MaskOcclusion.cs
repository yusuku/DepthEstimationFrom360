using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Assertions;
public class Slicenet2MaskOcclusion : MonoBehaviour
{
    // Model 
    public ModelAsset modelAsset;
    public Texture2D inputtex;
    Worker m_Worker;
    Tensor<float> tensor;
    public Material mat;

    //GPU Instansing
    public Texture2D Deffusemap;
    int instanceCount = 100000;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer ColorBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    Bounds boundingBox;

    void Start()
    {
        Texture2D out_texture= model2Text(modelAsset);
        out_texture=FlipTextureVertically(out_texture);
        mat.mainTexture = out_texture;
        Makeocclustion(out_texture, 50f);
        boundingBox = new Bounds(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(100.0f, 100.0f, 100.0f));

    }

    void Update()
    {
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, boundingBox, argsBuffer);
    }

    void OnDisable()
    {
        m_Worker.Dispose();
        tensor.Dispose();
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;

        if (ColorBuffer != null)
            ColorBuffer.Release();
        ColorBuffer = null;

    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(boundingBox.center, boundingBox.size);
    }
    public Texture2D FlipTextureVertically(Texture2D original)
    {
        int width = original.width;
        int height = original.height;

        // 元のピクセルデータを取得
        Color[] pixels = original.GetPixels();
        Color[] flippedPixels = new Color[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 上下反転のインデックスを計算
                int flippedIndex = (height - y - 1) * width + x;
                int originalIndex = y * width + x;

                flippedPixels[flippedIndex] = pixels[originalIndex];
            }
        }

        // 新しいテクスチャを作成して反転したピクセルを設定
        Texture2D flippedTexture = new Texture2D(width, height, original.format, false);
        flippedTexture.SetPixels(flippedPixels);
        flippedTexture.Apply();

        return flippedTexture;
    }
    //Sentis model
    Texture2D model2Text(ModelAsset modelAsset)
    {
        tensor = TextureConverter.ToTensor(inputtex, width: 1024, height: 512);

        var model = ModelLoader.Load(modelAsset);
        m_Worker = new Worker(model, BackendType.GPUCompute);
        m_Worker.Schedule(tensor);
        var outputTensor = m_Worker.PeekOutput("output") as Tensor<float>;

        Texture2D tex = TensorToTexture2D(outputTensor.ReadbackAndClone());
        //tex = CropTexture2D(tex, 70, 442);
        return tex;
    }
    Texture2D CropTexture2D(Texture2D original, int top, int bottom)
    {
        int width = original.width;
        int croppedHeight = bottom - top;

        Texture2D croppedTexture = new Texture2D(width, croppedHeight, original.format, false);


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
                float value = tensor[0, y, x]; 

                if (min > value) min = value;
                if (max < value) max = value;
            }
        }
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = tensor[0, y, x]; 
                value = (value - min) / (max - min);
                Color color = new Color(value, value, value, 1.0f); 
                texture.SetPixel(x, y, color);

            }
        }

        texture.Apply();
        return texture;
    }

    //Occlustion
    void Makeocclustion(Texture2D depthmap, float ruu)
    {
        Color[] pix = depthmap.GetPixels();
        Color[] colors = Deffusemap.GetPixels();
        int width = depthmap.width, height = depthmap.height;
        Debug.Log("width: " + width + "height: " + height);


        List<Vector4> pointData = new List<Vector4>();
        List<Color> ColorData = new List<Color>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (y >= 100 && y <= 400)
                {
                    var pic_dep = pix[x + y * width].r;
                    var pic = colors[x + y * width];
                    //if (pic_dep <= r && pic_dep != 0.0f)
                    {
                        Debug.Log(pic_dep);
                        Vector2 polar = XY2Polar(x, y, width, height);
                        float radius = pic_dep * ruu;
                        Vector3 position = PolarToCartesian(polar.x, polar.y, radius);
                        pointData.Add(new Vector4(position.x, position.y, position.z, 0.1f));
                        ColorData.Add(pic);


                    }
                }

            }
        }

        instanceCount = pointData.Count;

        Vector4[] positionsarray = pointData.ToArray();
        Color[] Colorarray = ColorData.ToArray();
        positionBuffer = new ComputeBuffer(instanceCount, 16);
        positionBuffer.SetData(positionsarray);
        ColorBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf<Color>());
        ColorBuffer.SetData(Colorarray);
        instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
        instanceMaterial.SetBuffer("ColorBuffer", ColorBuffer);


        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
        args[1] = (uint)instanceCount;
        args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
        args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);

        argsBuffer.SetData(args);

    }


    public static Vector3 PolarToCartesian(float phi, float theta, float radius = 1.0f)
    {
        float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
        float y = radius * Mathf.Cos(theta);
        float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);

        return new Vector3(x, y, z);
    }
    Vector2 XY2Polar(int x, int y, int width, int height)
    {
        Vector2 polar;

        polar = new Vector2((1 - x / (float)width) * 2 * Mathf.PI - Mathf.PI, Mathf.PI - y / (float)height * Mathf.PI);

        return polar;
    }
 
}
