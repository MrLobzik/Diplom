using UnityEngine;

public class AdvancedFPSCounter : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showFPS = true;
    public bool showMinMax = true;
    public bool showAverage = true;

    [Header("FPS Colors")]
    [Tooltip("Цвет для высокого FPS (>= 60)")]
    public Color highFPSColor = Color.green;

    [Tooltip("Цвет для среднего FPS (30-59)")]
    public Color mediumFPSColor = Color.yellow;

    [Tooltip("Цвет для низкого FPS (< 30)")]
    public Color lowFPSColor = Color.red;

    [Tooltip("Цвет для статистики (min/max/avg)")]
    public Color statsColor = Color.white;

    [Header("Font Settings")]
    [Range(12, 100)]
    public int fontSize = 24;

    [Header("Padding")]
    [Tooltip("Отступ от краев экрана")]
    public Vector2 padding = new Vector2(10, 10);

    [Tooltip("Отступ между строками")]
    public float lineSpacing = 5f;

    [Tooltip("Дополнительный отступ внутри фона")]
    public Vector2 backgroundPadding = new Vector2(10, 10);

    [Header("Other")]
    [Range(0.1f, 1f)]
    public float updateInterval = 0.5f;

    [Header("Background")]
    public bool showBackground = false;
    public Color backgroundColor = new Color(0, 0, 0, 0.5f);

    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private float minFPS = float.MaxValue;
    private float maxFPS = float.MinValue;
    private float avgFPS = 0.0f;
    private int frameCount = 0;
    private float totalFPS = 0.0f;

    private GUIStyle style;
    private GUIStyle backgroundStyle;
    private int linesCount = 0;

    // Кэшированные строки для расчета ширины
    private string fpsText = "";
    private string minMaxText = "";
    private string avgText = "";

    private void Start()
    {
        InitializeStyles();
        CalculateLineCount();
    }

    private void InitializeStyles()
    {
        style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;

        backgroundStyle = new GUIStyle();
        backgroundStyle.normal.background = MakeTex(2, 2, backgroundColor);
    }

    private void CalculateLineCount()
    {
        linesCount = 0;
        if (showFPS) linesCount++;
        if (showMinMax) linesCount++;
        if (showAverage) linesCount++;
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void Update()
    {
        // Проверяем, изменилось ли количество строк
        int newLinesCount = 0;
        if (showFPS) newLinesCount++;
        if (showMinMax) newLinesCount++;
        if (showAverage) newLinesCount++;

        if (newLinesCount != linesCount)
        {
            linesCount = newLinesCount;
        }

        // Обновляем FPS
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;

        // Обновляем статистику
        frameCount++;
        totalFPS += fps;
        avgFPS = totalFPS / frameCount;

        if (fps < minFPS) minFPS = fps;
        if (fps > maxFPS) maxFPS = fps;

        // Обновляем текстовые строки
        fpsText = string.Format("{0:0.} FPS", fps);
        minMaxText = string.Format("Min: {0:0.} | Max: {1:0.}", minFPS, maxFPS);
        avgText = string.Format("Avg: {0:0.}", avgFPS);

        // Сброс статистики по клавише
        if (Input.GetKeyDown(KeyCode.F5))
        {
            ResetStats();
        }
    }

    private Vector2 CalculateTextSize(string text)
    {
        // Создаем временный GUIStyle для измерения
        GUIStyle tempStyle = new GUIStyle(style);
        tempStyle.fontSize = fontSize;

        // Измеряем размер текста
        Vector2 size = tempStyle.CalcSize(new GUIContent(text));
        return size;
    }

    private Rect CalculateRect()
    {
        // Находим максимальную ширину среди всех строк
        float maxWidth = 0;

        if (showFPS)
        {
            Vector2 fpsSize = CalculateTextSize(fpsText);
            maxWidth = Mathf.Max(maxWidth, fpsSize.x);
        }

        if (showMinMax)
        {
            Vector2 minMaxSize = CalculateTextSize(minMaxText);
            maxWidth = Mathf.Max(maxWidth, minMaxSize.x);
        }

        if (showAverage)
        {
            Vector2 avgSize = CalculateTextSize(avgText);
            maxWidth = Mathf.Max(maxWidth, avgSize.x);
        }

        // Вычисляем высоту одной строки
        float lineHeight = fontSize + lineSpacing;

        // Общая высота для всех строк
        float totalHeight = lineHeight * linesCount;

        // Если нет строк, минимальный размер
        if (linesCount == 0)
        {
            maxWidth = 100;
            totalHeight = 20;
        }

        return new Rect(padding.x, padding.y, maxWidth, totalHeight);
    }

    private void OnGUI()
    {
        // Обновляем стиль если размер шрифта изменился
        if (style.fontSize != fontSize)
        {
            style.fontSize = fontSize;
        }

        Rect contentRect = CalculateRect();

        // Отрисовка фона с учетом padding
        if (showBackground && linesCount > 0)
        {
            Rect backgroundRect = new Rect(
                contentRect.x - backgroundPadding.x,
                contentRect.y - backgroundPadding.y,
                contentRect.width + backgroundPadding.x * 2,
                contentRect.height + backgroundPadding.y * 2
            );
            GUI.Box(backgroundRect, GUIContent.none, backgroundStyle);
        }

        float currentY = contentRect.y;

        // Отображаем FPS с цветом, зависящим от производительности
        if (showFPS)
        {
            Color fpsColor = GetFPSColor(fps);
            DrawText(fpsText, contentRect.x, currentY, contentRect.width, fpsColor);
            currentY += fontSize + lineSpacing;
        }

        // Отображаем Min/Max с отдельным цветом
        if (showMinMax)
        {
            DrawText(minMaxText, contentRect.x, currentY, contentRect.width, statsColor);
            currentY += fontSize + lineSpacing;
        }

        // Отображаем Average
        if (showAverage)
        {
            DrawText(avgText, contentRect.x, currentY, contentRect.width, statsColor);
        }
    }

    private void DrawText(string text, float x, float y, float width, Color color)
    {
        style.normal.textColor = color;
        Rect textRect = new Rect(x, y, width, fontSize + lineSpacing);
        GUI.Label(textRect, text, style);
    }

    private Color GetFPSColor(float currentFPS)
    {
        if (currentFPS >= 60)
            return highFPSColor;
        else if (currentFPS >= 30)
            return mediumFPSColor;
        else
            return lowFPSColor;
    }

    public void ResetStats()
    {
        minFPS = float.MaxValue;
        maxFPS = float.MinValue;
        avgFPS = 0.0f;
        frameCount = 0;
        totalFPS = 0.0f;
    }

    // Для отладки в редакторе
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            InitializeStyles();
        }

        // Обновляем количество строк при изменении настроек в редакторе
        CalculateLineCount();
    }
}

