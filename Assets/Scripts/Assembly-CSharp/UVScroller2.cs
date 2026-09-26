using UnityEngine;

public class UVScroller2 : MonoBehaviour
{
	public int targetMaterialSlot;
	public float speedY = 0.5f;
	public float speedX;
	public float speedY2 = 0.5f;
	public float speedX2;

	private float timeWentX;
	private float timeWentY;
	private float timeWentX2;
	private float timeWentY2;

	private void Start()
	{
	}

	private void Update()
	{
		Renderer component = GetComponent<Renderer>();
		if (component != null)
		{
			Material[] materials = component.materials;
			if (materials != null && targetMaterialSlot < materials.Length)
			{
				float deltaTime = Time.deltaTime;
				timeWentY += deltaTime * speedY;
				timeWentX += deltaTime * speedX;
				timeWentY2 += deltaTime * speedY2;
				timeWentX2 += deltaTime * speedX2;
				Material material = materials[targetMaterialSlot];
				if (material != null)
				{
					material.SetTextureOffset("_MainTex", new Vector2(timeWentX, timeWentY));
				}
			}
		}
	}

	public UVScroller2()
	{
	}
}
