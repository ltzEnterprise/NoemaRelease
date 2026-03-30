using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class OceanMirror : MonoBehaviour
{
    [Header("Configuração")]
    public RenderTexture texturaEspelho; 
    public LayerMask ignorarNoReflexo; 
    
    private Camera camReflexo;
    private Material materialAgua; 

    private void OnEnable() 
    { 
        RenderPipelineManager.beginCameraRendering += PrepararCamera; 
        RenderPipelineManager.endCameraRendering += LimparCamera; // A trava nova aqui!
    }
    
    private void OnDisable() 
    { 
        RenderPipelineManager.beginCameraRendering -= PrepararCamera; 
        RenderPipelineManager.endCameraRendering -= LimparCamera; 
        if(camReflexo) DestroyImmediate(camReflexo.gameObject); 
    }

    private void Update()
    {
        if (Camera.main != null)
        {
            Camera.main.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
        }
    }

    // --- A MÁGICA FOI CONSERTADA AQUI ---
    private void PrepararCamera(ScriptableRenderContext context, Camera cameraAtual)
    {
        // 1. Se quem está na fila agora é a NOSSA câmera de reflexo, inverte as faces na hora certa!
        if (cameraAtual == camReflexo)
        {
            GL.invertCulling = true;
            return;
        }

        // 2. Ignora se for a tela de preview da Unity
        if (texturaEspelho == null || cameraAtual.cameraType == CameraType.Reflection || cameraAtual.cameraType == CameraType.Preview) return;

        // 3. Prepara o reflexo da Câmera Principal
        if (materialAgua == null) materialAgua = GetComponent<Renderer>().sharedMaterial;
        if (materialAgua != null) materialAgua.SetTexture("_TexturaReflexoAgua", texturaEspelho);

        if (camReflexo == null)
        {
            GameObject go = new GameObject("CameraFantasma_OceanMirror");
            go.hideFlags = HideFlags.HideAndDontSave; 
            camReflexo = go.AddComponent<Camera>();
            camReflexo.cameraType = CameraType.Reflection;
            camReflexo.enabled = false;
            
            UniversalAdditionalCameraData camData = camReflexo.GetUniversalAdditionalCameraData();
            if (camData != null)
            {
                camData.renderShadows = false; // Garante que a câmera fantasma não crie sombras extras
                camData.renderPostProcessing = false;
            }
        }

        camReflexo.CopyFrom(cameraAtual);
        camReflexo.targetTexture = texturaEspelho;
        camReflexo.cullingMask = ~ignorarNoReflexo;

        Vector3 posAgua = transform.position;
        Vector3 normalAgua = transform.up;
        float d = -Vector3.Dot(normalAgua, posAgua);
        Vector4 planoEspelho = new Vector4(normalAgua.x, normalAgua.y, normalAgua.z, d);
        
        camReflexo.worldToCameraMatrix = cameraAtual.worldToCameraMatrix * CalcularMatriz(planoEspelho);

        Vector4 clipPlane = CameraSpacePlane(camReflexo, posAgua, normalAgua, 1.0f);
        camReflexo.projectionMatrix = cameraAtual.CalculateObliqueMatrix(clipPlane);

        // Manda a câmera pra fila do Unity 6. O GL.invertCulling ali de cima vai agir no momento perfeito.
        UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
        request.destination = texturaEspelho;
        RenderPipeline.SubmitRenderRequest(camReflexo, request);
    }

    // 4. Quando a câmera fantasma termina a vez dela na fila, desliga a inversão pra não cagar o resto do jogo
    private void LimparCamera(ScriptableRenderContext context, Camera cameraAtual)
    {
        if (cameraAtual == camReflexo)
        {
            GL.invertCulling = false;
        }
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * 0.05f;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    private Matrix4x4 CalcularMatriz(Vector4 plano)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = (1F - 2F * plano[0] * plano[0]); m.m01 = (-2F * plano[0] * plano[1]); m.m02 = (-2F * plano[0] * plano[2]); m.m03 = (-2F * plano[3] * plano[0]);
        m.m10 = (-2F * plano[1] * plano[0]); m.m11 = (1F - 2F * plano[1] * plano[1]); m.m12 = (-2F * plano[1] * plano[2]); m.m13 = (-2F * plano[3] * plano[1]);
        m.m20 = (-2F * plano[2] * plano[0]); m.m21 = (-2F * plano[2] * plano[1]); m.m22 = (1F - 2F * plano[2] * plano[2]); m.m23 = (-2F * plano[3] * plano[2]);
        return m;
    }
}