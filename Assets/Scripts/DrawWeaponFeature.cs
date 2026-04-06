using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class DrawWeaponFeature : ScriptableRendererFeature
{
    public LayerMask itemLayer;

    class DrawWeaponPass : ScriptableRenderPass
    {
        public LayerMask layerMask;
        private FilteringSettings filteringSettings;
        private ShaderTagId shaderTagId = new ShaderTagId("UniversalForward");

        public DrawWeaponPass(LayerMask layer)
        {
            layerMask = layer;
            // Filtra para desenhar apenas a layer do item
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
        }

        class PassData
        {
            public RendererListHandle rendererList;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();

            RenderTextureDescriptor cameraDesc = cameraData.cameraTargetDescriptor;

            TextureDesc depthDesc = new TextureDesc(cameraDesc.width, cameraDesc.height);
            depthDesc.dimension = cameraDesc.dimension;       
            depthDesc.msaaSamples = (MSAASamples)cameraDesc.msaaSamples;   
            depthDesc.slices = cameraDesc.volumeDepth;        
            depthDesc.depthBufferBits = DepthBits.Depth32;
            depthDesc.name = "WeaponTempDepth";
            depthDesc.clearBuffer = true;

            TextureHandle tempDepth = renderGraph.CreateTexture(depthDesc);

            SortingSettings sortingSettings = new SortingSettings(cameraData.camera);
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            
            // Garante que o item vai receber luzes e sombras em qualquer shader moderno
            drawingSettings.SetShaderPassName(1, new ShaderTagId("UniversalForwardOnly"));
            drawingSettings.SetShaderPassName(2, new ShaderTagId("LightweightForward"));
            drawingSettings.SetShaderPassName(3, new ShaderTagId("SRPDefaultUnlit"));

            RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            RendererListHandle rendererList = renderGraph.CreateRendererList(listParams);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Item Over Walls", out var passData))
            {
                passData.rendererList = rendererList;

                // 3. Pinta na cor principal, mas usa o Buffer falso para a profundidade
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(tempDepth, AccessFlags.Write);
                
                builder.UseRendererList(rendererList);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }
    }

    DrawWeaponPass pass;

    public override void Create()
    {
        pass = new DrawWeaponPass(itemLayer);
        // Dispara depois de desenhar a parede, pra pintar por cima
        pass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques; 
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
}