using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class 绘图 : Graphic
    {
        [System.Serializable]
        public class PointRange
        {
            public float minX = 0f;
            public float maxX = 100f;
            public float minY = -100f;
            public float maxY = 100f;
        }

        public InputField[] inputFieldsX = new InputField[11]; // X coordinates for 10 points
        public InputField[] inputFieldsY = new InputField[11]; // Y coordinates for 10 points
        public PointRange[] pointRanges = new PointRange[11]; // Individual ranges for each point
        public Button drawButton;

        private Vector2[] points = new Vector2[12]; // Origin + 10 points
        private Color[] lineColors = new Color[] {
            Color.red, Color.blue, Color.green, Color.yellow,
            Color.cyan, Color.magenta, Color.gray, new Color(1, 0.5f, 0),
            new Color(0.5f, 0, 1), new Color(0, 0.5f, 0.5f),Color.red
        };

        private Vector2 originPosition = new Vector2(20f, 20f); // 新的原点位置

        protected override void Start()
        {
            base.Start();
            // 初始化所有点为原点
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = originPosition;
            }
            // 确保数组不为null
            if (inputFieldsX == null) inputFieldsX = new InputField[11];
            if (inputFieldsY == null) inputFieldsY = new InputField[11];
            if (pointRanges == null) pointRanges = new PointRange[11];

            // 初始化默认范围
            for (int i = 0; i < 11; i++)
            {
                if (pointRanges[i] == null)
                {
                    pointRanges[i] = new PointRange
                    {
                        minX = 0f,
                        maxX = 100f,
                        minY = -100f,
                        maxY = 100f
                    };
                }
            }

            // 初始化原点
            points[0] = originPosition;

            // 添加监听器
            for (int i = 0; i < 11; i++)
            {
                if (inputFieldsX[i] != null)
                {
                    inputFieldsX[i].onValidateInput += ValidateInput;
                    inputFieldsX[i].contentType = InputField.ContentType.DecimalNumber;
                }

                if (inputFieldsY[i] != null)
                {
                    inputFieldsY[i].onValidateInput += ValidateInput;
                    inputFieldsY[i].contentType = InputField.ContentType.DecimalNumber;
                }
            }

            drawButton.onClick.AddListener(UpdatePointsAndDraw);
        }

        private void UpdatePointsAndDraw()
        {
            // Update all points' positions
            for (int i = 0; i < 11; i++)
            {
                if (inputFieldsX[i] != null && inputFieldsY[i] != null && pointRanges[i] != null)
                {
                    float x = Mathf.Clamp(ParseInput(inputFieldsX[i].text), pointRanges[i].minX, pointRanges[i].maxX)*0.8f;
                    float y = Mathf.Clamp(ParseInput(inputFieldsY[i].text), pointRanges[i].minY, pointRanges[i].maxY) * 0.08f;

                    if (i == 0)
                    {
                        // First point: absolute X and Y (relative to origin)
                        points[i + 1] = new Vector2(originPosition.x + x, originPosition.y + y);
                    }
                    else
                    {
                        // Subsequent points:
                        // - X: absolute position (no offset)
                        // - Y: previous Y + current input Y
                        points[i + 1] = new Vector2(
                            points[i].x + x,  // Absolute X position
                            originPosition.y + y  // Y position relative to origin
                        );
                    }
                }
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            DrawAxes(vh);

            // Draw all line segments
            for (int i = 0; i < 11; i++)
            {
                if (inputFieldsX[i] != null && inputFieldsY[i] != null &&
                    !string.IsNullOrEmpty(inputFieldsX[i].text) &&
                    !string.IsNullOrEmpty(inputFieldsY[i].text))
                {
                    DrawLine(vh, points[i], points[i + 1], lineColors[i % lineColors.Length]);
                }
            }
        }

        private void DrawAxes(VertexHelper vh)
        {
            Rect rect = GetPixelAdjustedRect();
            float width = rect.width;
            float height = rect.height;

            // X axis (正半轴)
            Vector2 xAxisStart = originPosition;
            Vector2 xAxisEnd = new Vector2(width * 1.12f, originPosition.y);
            DrawLine(vh, xAxisStart, xAxisEnd, Color.black);

            // Y axis (正半轴)
            Vector2 yAxisStart = originPosition;
            Vector2 yAxisEnd = new Vector2(originPosition.x, height * 2.3f);
            DrawLine(vh, yAxisStart, yAxisEnd, Color.black);
        }

        private void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, Color color)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * 0.5f;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = start - perpendicular;
            vh.AddVert(vertex);

            vertex.position = start + perpendicular;
            vh.AddVert(vertex);

            vertex.position = end + perpendicular;
            vh.AddVert(vertex);

            vertex.position = end - perpendicular;
            vh.AddVert(vertex);

            int vertexIndex = vh.currentVertCount - 4;
            vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vh.AddTriangle(vertexIndex + 2, vertexIndex + 3, vertexIndex);
        }

        private float ParseInput(string input)
        {
            if (float.TryParse(input, out float result))
            {
                return result;
            }
            return 0;
        }

        private char ValidateInput(string text, int charIndex, char addedChar)
        {
            // 允许数字、负号(仅在开头)、小数点(不允许连续)
            if (char.IsDigit(addedChar))
            {
                return addedChar;
            }
            else if (addedChar == '-' && charIndex == 0)
            {
                return addedChar; // 只允许在开头输入负号
            }
            else if (addedChar == '.' && !text.Contains("."))
            {
                return addedChar; // 只允许一个小数点
            }
            return '\0';
        }

    }}
