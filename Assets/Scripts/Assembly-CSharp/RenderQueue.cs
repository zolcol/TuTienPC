using UnityEngine;

[AddComponentMenu("FTGame/RenderQueue")]
public class RenderQueue : MonoBehaviour
{
	public int[] m_nRenderQueue;

	private void Start()
	{
		Renderer component = base.GetComponent<Renderer>();
		if (component == null)
		{
			return;
		}
		Material[] materials = component.materials;
		if (materials == null || this.m_nRenderQueue == null)
		{
			return;
		}
		if (materials.Length != this.m_nRenderQueue.Length || materials.Length < 1)
		{
			return;
		}
		for (int i = 0; i < materials.Length; i++)
		{
			if (this.m_nRenderQueue[i] != 0)
			{
				Material material = materials[i];
				if (material != null)
				{
					int renderQueue = material.renderQueue;
					material.renderQueue = renderQueue + this.m_nRenderQueue[i];
				}
			}
		}
	}
}
