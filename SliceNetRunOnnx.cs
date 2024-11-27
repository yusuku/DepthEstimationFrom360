using Unity.Sentis;
using UnityEngine;
using UnityEngine.Assertions;
public class RunOnnx : MonoBehaviour
{
    [SerializeField]
    ModelAsset modelAsset;
    Worker m_Worker;
    Tensor m_Inputs;
    Tensor<float> tensorTexture;
    [SerializeField]
    Texture2D inputtexture;
    [SerializeField]
    RenderTexture rRenderTexture;

    void OnEnable()
    {
        tensorTexture = TextureConverter.ToTensor(inputtexture, width: 1024, height: 512);
        var model = ModelLoader.Load(modelAsset);
        m_Worker = new Worker(model, BackendType.GPUCompute);

        // The MultipleInputMultipleOuput model takes two inputs, "input0" and "input1"
        // since it has multiple inputs, we need to use a dictionary tensorName -> tensor
        m_Inputs = new Tensor<float>(new TensorShape(1,3,512,1024));
        
        
    }

    void Update()
    {
        m_Worker.Schedule(tensorTexture);

        // model has multiple output, so to know which output to get we need to specify which one we are referring to
        var outputTensor0 = m_Worker.PeekOutput("output") as Tensor<float>;
   

        // If you wish to read from the tensor, download it to cpu.
        // See async examples for non-blocking readback.
        var cpuTensor0 = outputTensor0.ReadbackAndClone();
        cpuTensor0.Reshape(new TensorShape(1, 512, 1024, 1));
        Debug.Log(cpuTensor0.shape);
        TextureConverter.RenderToTexture(cpuTensor0, rRenderTexture, new TextureTransform().SetBroadcastChannels(true));

    }

    void OnDisable()
    {
        // Clean up Sentis resources.
        m_Worker.Dispose();
        m_Inputs.Dispose();
        tensorTexture.Dispose();
    }
}