using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace Beam.PostEffects;

public class SunshaftsEffect : PostProcessEffectRenderer<SunshaftsSettings>
{
	public enum Pass
	{
		Screen,
		RadialBlur,
		Depth
	}

	public override void Render(PostProcessRenderContext context)
	{
		CommandBuffer command = context.command;
		command.BeginSample("SunshaftsEffect");
		int width = context.width / (int)settings.Downsampling;
		int height = context.height / (int)settings.Downsampling;
		Vector3 vector = context.camera.WorldToViewportPoint(Singleton<Atmosphere>.Instance.CurrentIlluminary.Transform.position);
		PropertySheet propertySheet = context.propertySheets.Get("Beam Team/Post Effects/Sunshafts");
		propertySheet.properties.SetVector("_BlurRadius4", new Vector4(1f, 1f, 0f, 0f) * settings.BlurRadius);
		propertySheet.properties.SetVector("_SunPosition", new Vector4(vector.x, vector.y, vector.z, settings.DistanceFalloff));
		propertySheet.properties.SetVector("_SunThreshold", new Color(0.87f, 0.74f, 0.65f));
		float num = (float)settings.BlurRadius * 0.0013020834f;
		propertySheet.properties.SetVector("_BlurRadius4", new Vector4(num, num, 0f, 0f));
		int num2 = Shader.PropertyToID("_SunshaftsBlitSource");
		command.GetTemporaryRT(num2, width, height, 0);
		command.BlitFullscreenTriangle(context.source, num2, propertySheet, 2);
		int num3 = 0;
		int num4 = settings.BlurIterations;
		for (int i = 0; i < num4; i++)
		{
			int num5 = Shader.PropertyToID("_SunshaftsIteration" + num3++);
			command.GetTemporaryRT(num5, width, height, 0);
			command.BlitFullscreenTriangle(num2, num5, propertySheet, 1);
			command.ReleaseTemporaryRT(num2);
			num = (float)settings.BlurRadius * (((float)i * 2f + 1f) * 6f) / 768f;
			propertySheet.properties.SetVector("_BlurRadius4", new Vector4(num, num, 0f, 0f));
			num2 = Shader.PropertyToID("_SunshaftsIteration" + num3++);
			command.GetTemporaryRT(num2, width, height, 0);
			command.BlitFullscreenTriangle(num5, num2, propertySheet, 1);
			command.ReleaseTemporaryRT(num5);
			num = (float)settings.BlurRadius * (((float)i * 2f + 2f) * 6f) / 768f;
			propertySheet.properties.SetVector("_BlurRadius4", new Vector4(num, num, 0f, 0f));
		}
		Color linear = Singleton<Atmosphere>.Instance.CurrentIlluminary.Light.color.linear;
		Vector4 value = ((vector.z >= 0f) ? (new Vector4(linear.r, linear.g, linear.b, linear.a) * settings.Intensity) : Vector4.zero);
		propertySheet.properties.SetVector("_SunColor", value);
		command.SetGlobalTexture("_ColorBuffer", num2);
		command.BlitFullscreenTriangle(context.source, context.destination, propertySheet, 0);
		command.ReleaseTemporaryRT(num2);
		command.EndSample("SunshaftsEffect");
	}
}
