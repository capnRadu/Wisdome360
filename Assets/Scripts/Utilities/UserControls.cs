using UnityEngine;

public class UserControls : MonoBehaviour
{
    public GameObject MainCamera;
    public GameObject DomeRig;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            MainCamera.SetActive(true);
            DomeRig.SetActive(false);

        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            MainCamera.SetActive(false);
            DomeRig.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}