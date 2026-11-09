using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIGradient : BaseMeshEffect
{
    [SerializeField] private Color topColor = new Color(0, 0, 0, 0.6f);
    [SerializeField] private Color bottomColor = new Color(0, 0, 0, 0);

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        UIVertex vertex = new UIVertex();

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            float t = vertex.position.y;
            t = Mathf.InverseLerp(-GetComponent<RectTransform>().rect.height / 2,
                GetComponent<RectTransform>().rect.height / 2, t);

            vertex.color = Color.Lerp(bottomColor, topColor, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}