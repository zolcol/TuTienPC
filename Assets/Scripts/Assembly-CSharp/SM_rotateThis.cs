using UnityEngine;

public class SM_rotateThis : MonoBehaviour
{
	public float rotationSpeedX;
	public float rotationSpeedY;
	public float rotationSpeedZ;
	private Vector3 rotationVector;

	private void Update()
	{
		base.transform.Rotate(this.rotationSpeedX * Time.deltaTime, this.rotationSpeedY * Time.deltaTime, this.rotationSpeedZ * Time.deltaTime);
	}
}
