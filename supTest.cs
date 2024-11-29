using Unity.Sentis;
using UnityEngine;
using UnityEngine.Assertions;

public class supTest : MonoBehaviour
{
    public ModelAsset modelAsset;
   
    Worker m_Worker;
    // Start is called before the first frame update
    void Start()
    {
        Tensor<float> tensor = new Tensor<float>(new TensorShape(1,1,28,28));
        for(int i = 0; i < 28; i++)
        {
            for(int j = 0; j < 28; j++)
            {
                tensor[0, 0, i, j] = 1f;
            }
        }
        float[]  tesarray = tensor.DownloadToArray();
        for (int i = 0; i < tesarray.Length; i++)
        {
            Debug.Log("i: " + i + " tensor; " + tesarray[i]);
        }
   
        var model = ModelLoader.Load(modelAsset);
        m_Worker = new Worker(model, BackendType.GPUCompute);
        m_Worker.Schedule(tensor);
        var outputTensor = m_Worker.PeekOutput("output") as Tensor<float>;
        var array = outputTensor.DownloadToArray();
        for(int i=0;i<array.Length;i++)
        {
            Debug.Log("i: "+i+" v; "+array[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
