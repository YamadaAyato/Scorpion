using UnityEngine;
using UnityEngine.UI;

public class PlayerUIBase : MonoBehaviour
{
    [Header("CharacterInfo")]
    [SerializeField] private CharacterInfo _info;

    [Header("絵")]
    [SerializeField] private Image _charaImage;

    [Header("オブジェクトの位置")]
    [SerializeField] private Transform _charaTransform;

    private int _index = 0;
    private GameObject _currentObject;
    private bool _isGo;

    public bool IsGo => _isGo;

    private void Start() => CreateObject();

    protected void Receive(string name)
    {
        switch (name)
        {
            case "Left":

                _isGo = false;
                if (_index > 0)
                    _index--;
                else
                    _index = _info.CharaList.Count - 1;

                CreateObject();
                break;

            case "Right":

                _isGo = false;
                if (_index < _info.CharaList.Count - 1)
                    _index++;
                else
                    _index = 0;

                CreateObject();
                break;

            case "Decision":
                _isGo = true;
                break;
        }
    }

    protected void WReceive(string name)
    {
        switch (name)
        {
            case "Return":
                break;
        }
    }

    private void CreateObject()
    {
        _charaImage.sprite = _info.CharaList[_index].Sprite;

        if (_currentObject != null)
        {
            var c = _currentObject;
            Destroy(c);
            _currentObject = null;
        }

        _currentObject = Instantiate(_info.CharaList[_index].Character, _charaTransform);
    }
}
