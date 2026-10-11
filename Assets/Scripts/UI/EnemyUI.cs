using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyBTController controller;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Slider healthBar;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;

        if (controller == null)
            controller = GetComponentInParent<EnemyBTController>();

        if (health == null)
            health = GetComponentInParent<EnemyHealth>();
    }

    private void LateUpdate()
    {
        // Billboard: selalu menghadap kamera
        if (cam != null)
            transform.forward = cam.transform.forward;

        if (controller != null && stateText != null)
        {
            EnemyBTController.EnemyAction action = controller.CurrentAction;

            stateText.text = action.ToString();
            stateText.color = ActionColor(action);
        }

        if (health != null && healthBar != null)
            healthBar.value = health.CurrentHealth / health.MaxHealth;
    }

    private Color ActionColor(EnemyBTController.EnemyAction action)
    {
        switch (action)
        {
            case EnemyBTController.EnemyAction.Patrol: return Color.green;
            case EnemyBTController.EnemyAction.Chase: return Color.yellow;
            case EnemyBTController.EnemyAction.Attack: return Color.red;
            case EnemyBTController.EnemyAction.Flee: return Color.cyan;
            case EnemyBTController.EnemyAction.Search: return new Color(1f, 0.5f, 0f);
            default: return Color.gray;
        }
    }
}
