using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
public class MidasModel : MonoBehaviour
{
    // Model 
    public ModelAsset modelAsset;
    public RenderTexture inputtex;
    public RenderTexture outputTexture;
    Worker m_Worker;
    Tensor<float> inputTensor;

    public Material mat;


   

    void Start()
    {
        
        
    }

    void Update()
    {
        model2Text(modelAsset, inputtex);
    }

    void OnDisable()
    {
     
    }

   
    void model2Text(ModelAsset modelAsset, RenderTexture inputtex)
    {
        inputTensor = TextureConverter.ToTensor(inputtex, width: 256, height: 256,channels: 3);

        Model model = ModelLoader.Load(modelAsset);
        m_Worker = new Worker(model, BackendType.GPUCompute);
        m_Worker.Schedule(inputTensor);
        Tensor<float> outputTensor = m_Worker.PeekOutput() as Tensor<float>;
        outputTensor.Reshape(new TensorShape(1,outputTensor.shape[0],outputTensor.shape[1], outputTensor.shape[2]));
        Debug.Log(outputTensor.shape);
        mat.mainTexture= TextureConverter.ToTexture(outputTensor);
        
    }
    
}
