using TMPro;
using UnityEngine;
using Zellige.App;

namespace Zellige.UI
{
    /// <summary>
    /// Shows the text of a <see cref="TextKeys"/> key on the label it sits on (D8). Scenes store
    /// the key, not the English sentence, so another language later needs no scene edits.
    /// The builders also write the English text into the label, so it reads correctly in the Editor.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        public string Key => key;

        private void Awake() => Apply();

        public void Apply()
        {
            var label = GetComponent<TMP_Text>();
            if (label != null && !string.IsNullOrEmpty(key)) label.text = TextCatalog.Get(key);
        }

        /// <summary>Gives <paramref name="label"/> the text of <paramref name="textKey"/> now and on every load.</summary>
        public static LocalizedText Attach(TMP_Text label, string textKey)
        {
            var localized = label.GetComponent<LocalizedText>();
            if (localized == null) localized = label.gameObject.AddComponent<LocalizedText>();   // no "??": Unity fakes null
            localized.key = textKey;
            localized.Apply();
            return localized;
        }
    }
}
