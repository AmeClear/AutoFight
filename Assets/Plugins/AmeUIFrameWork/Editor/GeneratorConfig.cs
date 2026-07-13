using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum GeneratorType
{
    Find,//组件查找
    Bind,//组件绑定
}
public enum ParseType
{
    Name,
    Tag
}
[CreateAssetMenu(fileName = "GeneratorConfig", menuName = "GeneratorConfig", order = 0)]
public class GeneratorConfig : ScriptableObject
{

    private static GeneratorConfig _instance;
    public static GeneratorConfig Instance { get { if (_instance == null) { _instance = Resources.Load<GeneratorConfig>("GeneratorConfig"); } return _instance; } }
    public string BindComponentGeneratorPath;
    public string FindComponentGeneratorPath;
    public string WindowGeneratorPath;
    public string OBJDATALIST_KEY = "objDataList";
    public GeneratorType GeneratorType = GeneratorType.Bind;
    public ParseType ParseType = ParseType.Name;
    public string[] TAGArr = { "Image", "RawImage", "Text", "Button", "Slider", "Dropdown", "InputField", "Canvas", "Panel", "ScrollRect", "Toggle" };
}