using UnityEngine;

public class BattleHudSafeArea : MonoBehaviour
{
    public Camera uiCamera;
    public Transform leftHud;
    public Transform rightHud;
    public float extraPaddingPixels = 32f;

    private Rect previousSafeArea;
    private int previousWidth = -1;
    private int previousHeight = -1;
    private Vector3 leftOffset;
    private Vector3 rightOffset;

    private void LateUpdate()
    {
        Rect safe = Screen.safeArea;

        if (previousWidth == Screen.width &&
            previousHeight == Screen.height &&
            previousSafeArea.Equals(safe))
        {
            return;
        }

        previousWidth = Screen.width;
        previousHeight = Screen.height;
        previousSafeArea = safe;

        float leftInset = safe.xMin + extraPaddingPixels;
        float rightInset = Screen.width - safe.xMax + extraPaddingPixels;

        ApplyOffset(leftHud, ref leftOffset, GetInset(leftHud, leftInset, true));
        ApplyOffset(rightHud, ref rightOffset, GetInset(rightHud, rightInset, false));
    }

    private Vector3 GetInset(Transform target, float pixels, bool fromLeft)
    {
        if (target == null || uiCamera == null || pixels <= 0f)
        {
            return Vector3.zero;
        }

        float depth = Vector3.Dot(
            target.position - uiCamera.transform.position,
            uiCamera.transform.forward);

        float edgeX = fromLeft ? 0f : Screen.width;
        float safeX = fromLeft ? pixels : Screen.width - pixels;
        float y = Screen.height * 0.5f;

        Vector3 edge = uiCamera.ScreenToWorldPoint(
            new Vector3(edgeX, y, depth));
        Vector3 safeEdge = uiCamera.ScreenToWorldPoint(
            new Vector3(safeX, y, depth));

        return safeEdge - edge;
    }

    private void ApplyOffset(
        Transform target,
        ref Vector3 previousOffset,
        Vector3 newOffset)
    {
        if (target != null)
        {
            target.position += newOffset - previousOffset;
        }

        previousOffset = newOffset;
    }
}
