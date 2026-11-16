using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElRetirado.Enemies
{
    public class PlayerHealth : MonoBehaviour
    {
        public float maximumHealth = 100;
        public UnityEvent onDeath = new UnityEvent();
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        Canvas hud;
        Text healthLabel;
        GameObject deathPanel;
        Image damageFlash;
        float flashUntil;
        void Awake() { CurrentHealth = maximumHealth; BuildHUD(); }
        void Update()
        {
            if (damageFlash && !IsDead) damageFlash.color = new Color(0.7f, 0, 0, Mathf.Clamp01((flashUntil - Time.unscaledTime) / 0.35f) * 0.4f);
        }
        public void TakeDamage(float damage)
        {
            if (IsDead || damage <= 0 || Time.timeScale <= 0) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            if (healthLabel) healthLabel.text = "Vida: " + Mathf.CeilToInt(CurrentHealth);
            flashUntil = Time.unscaledTime + 0.35f;
            if (CurrentHealth > 0) return;
            IsDead = true;
            foreach (var movement in GetComponentsInChildren<FPSController>()) movement.enabled = false;
            foreach (var bob in GetComponentsInChildren<HeadBobSystem>()) bob.enabled = false;
            foreach (var emitter in GetComponentsInChildren<PlayerNoiseEmitter>()) emitter.enabled = false;
            foreach (var pause in FindObjectsByType<PauseMenu>(FindObjectsSortMode.None)) pause.enabled = false;
            foreach (var tutorial in FindObjectsByType<TutorialManager>(FindObjectsSortMode.None)) tutorial.enabled = false;
            Time.timeScale = 0;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            deathPanel.SetActive(true);
            damageFlash.color = Color.clear;
            onDeath.Invoke();
        }
        void BuildHUD()
        {
            var root = new GameObject("HUD_Vida_y_Muerte", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            hud = root.GetComponent<Canvas>(); hud.renderMode = RenderMode.ScreenSpaceOverlay; hud.sortingOrder = 32760;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            damageFlash = Panel("Impacto", root.transform, Color.clear).GetComponent<Image>(); damageFlash.raycastTarget = false;
            healthLabel = Label("Vida", root.transform, "Vida: " + Mathf.CeilToInt(CurrentHealth), 25, new Vector2(0, 1), new Vector2(110, -45), new Vector2(180, 50));
            healthLabel.alignment = TextAnchor.MiddleLeft;
            deathPanel = Panel("Pantalla_Muerte", root.transform, new Color(0.025f, 0.015f, 0.015f, 0.96f));
            Label("Titulo", deathPanel.transform, "La bestia te ha atrapado", 48, new Vector2(0.5f, 0.5f), new Vector2(0, 65), new Vector2(1000, 100));
            var buttonObject = new GameObject("Reintentar", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(deathPanel.transform, false);
            Place(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(280, 65));
            buttonObject.GetComponent<Image>().color = new Color(0.45f, 0.08f, 0.08f);
            buttonObject.GetComponent<Button>().onClick.AddListener(Restart);
            Label("Texto", buttonObject.transform, "Reintentar", 28, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 65));
            deathPanel.SetActive(false);
        }
        static GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color; return go;
        }
        static Text Label(string name, Transform parent, string value, int size, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, dimensions);
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = size; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }
        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 dimensions)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = dimensions; }
        public void Restart()
        {
            Time.timeScale = 1; AudioListener.pause = false;
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }
}
