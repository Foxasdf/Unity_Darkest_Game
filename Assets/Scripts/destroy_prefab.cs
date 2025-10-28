using UnityEngine;

public class DestroyAfterAnim : MonoBehaviour
{
	void Start()
	{
		Destroy(gameObject, GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length);
	}
}
