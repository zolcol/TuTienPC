using UnityEngine;

public class Rotate : MonoBehaviour
{
	public float Speed;
	public float Rot;
	private float bottom;

	private void Awake()
	{
		this.bottom = base.transform.position.y;
	}

	private void Update()
	{
		base.transform.Rotate(0f, this.Rot * Time.deltaTime, 0f);
	}
}
