using UnityEngine;
using UnityEngine.UIElements;

public static class UIExtentions
{
    // Start is called before the first frame update
    public static void Display(this VisualElement element, bool enabled)
    {
        if (element == null)
            return;

        element.style.display = enabled? DisplayStyle.Flex: DisplayStyle.None;
    }
}
