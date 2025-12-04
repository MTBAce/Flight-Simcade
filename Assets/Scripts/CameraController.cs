using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] Camera[] cameras;
    private int index = 0;

    void Start()
    {
        ActivateCamera(index);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { index = 0; ActivateCamera(index); }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) { index = 1; ActivateCamera(index); }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) { index = 2; ActivateCamera(index); }
        else if (Input.GetKeyDown(KeyCode.Alpha4)) { index = 3; ActivateCamera(index); }
    }

    void ActivateCamera(int idx)
    {
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].enabled = (i == idx);
    }
}