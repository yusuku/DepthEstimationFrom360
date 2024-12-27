using Unity.Sentis;
using UnityEngine;

public class DepthRefine : MonoBehaviour
{
    public Texture2D inputTexture;
    //Midas
    public ModelAsset modelAsset;
    public Material Debugmat;
    MidasEstimation midas;

    RenderTexture outputTexture;
    
    //Meshinstance
    public Material Instancematerial;
    public Mesh Instancemesh;
    public ComputeShader InstanceShader;
    TextureDepthGPUInstancing GpuInstancing;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        midas=new MidasEstimation(modelAsset,Debugmat);
        Debug.Log("inWidth: " + inputTexture.width + " inHeight: " + inputTexture.height);

        outputTexture=midas.inference(inputTexture);
        Debug.Log("outWidth: " + outputTexture.width + " outHeight: " + outputTexture.height);
        GpuInstancing = new TextureDepthGPUInstancing(Instancematerial, Instancemesh, InstanceShader, inputTexture, outputTexture);
    }

    // Update is called once per frame
    void Update()
    {
        GpuInstancing.DrawMeshes(outputTexture);
    }
    private void OnDestroy()
    {
        midas.Release();
        GpuInstancing.Release();
    }
}
