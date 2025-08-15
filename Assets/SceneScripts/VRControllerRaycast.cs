using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRControllerRaycast : MonoBehaviour
{
    [SerializeField] private Transform controllerTransform; // The controller transform
    [SerializeField] private float maxRayDistance = 10f; // Raycast distance
    [SerializeField] private LayerMask uiLayer; // Layer for UI objects

    private RaycastHit hit;
    private GameObject currentHitObject;

    void Update()
    {
        // Raycast from the controller to detect UI
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        if (Physics.Raycast(ray, out hit, maxRayDistance, uiLayer))
        {
            // If we hit a UI element (slider)
            if (hit.collider.CompareTag("Slider"))
            {
                currentHitObject = hit.collider.gameObject;

                // Get the Slider component
                Slider slider = currentHitObject.GetComponent<Slider>();
                if (slider != null)
                {
                    // Update slider's value based on controller's interaction (trigger press or direct ray interaction)
                    slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, hit.distance / maxRayDistance);
                }
            }
        }
        else
        {
            currentHitObject = null;
        }
    }
}
