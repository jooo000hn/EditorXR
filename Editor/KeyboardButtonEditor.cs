using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(KeyboardButton))]
public class KeyboardButtonEditor : Editor
{
	const char kLowercaseStart = 'a';
	const char kLowercaseEnd = 'z';

	SerializedProperty m_CharacterProperty;
	SerializedProperty m_UseShiftCharacterProperty;
	SerializedProperty m_ShiftCharacterProperty;
	SerializedProperty m_ButtonTextProperty;
	SerializedProperty m_MatchButtonTextToCharacterProperty;
	SerializedProperty m_ButtonMeshProperty;
	SerializedProperty m_ButtonGraphicProperty;
	SerializedProperty m_RepeatOnHoldProperty;
	
	KeyboardButton m_KeyboardButton;
	bool m_ShiftCharIsUppercase;

	protected void OnEnable()
	{
		m_CharacterProperty = serializedObject.FindProperty("m_Character");
		m_UseShiftCharacterProperty = serializedObject.FindProperty("m_UseShiftCharacter");
		m_ShiftCharacterProperty = serializedObject.FindProperty("m_ShiftCharacter");
		m_ButtonTextProperty = serializedObject.FindProperty("m_TextComponent");
		m_MatchButtonTextToCharacterProperty = serializedObject.FindProperty("m_MatchButtonTextToCharacter");
		m_ButtonMeshProperty = serializedObject.FindProperty("m_TargetMesh");
		m_ButtonGraphicProperty = serializedObject.FindProperty("m_TargetGraphic");
		m_RepeatOnHoldProperty = serializedObject.FindProperty("m_RepeatOnHold");
	}

	public override void OnInspectorGUI()
	{
		m_KeyboardButton = (KeyboardButton)target;
		if (GUILayout.Button("Create layout parent"))
		{
			var i = 0;
			foreach (var child in m_KeyboardButton.transform.parent)
			{
				if (child == m_KeyboardButton.transform)
					break;
				i++;
			}

			var t = new GameObject(m_KeyboardButton.name + "_LayoutPosition");
			t.transform.SetParent(m_KeyboardButton.transform);
			t.transform.localPosition = Vector3.zero;
			t.transform.SetParent(m_KeyboardButton.transform.parent);
			t.transform.localScale = Vector3.one;
			m_KeyboardButton.transform.parent = t.transform;
			t.transform.SetSiblingIndex(i);
		}

		serializedObject.Update();

		CharacterField("Primary Character", m_CharacterProperty);

		EditorGUILayout.PropertyField(m_ButtonTextProperty);
		// Set text component to character
		if (m_KeyboardButton.textComponent != null)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_MatchButtonTextToCharacterProperty);
			if (EditorGUI.EndChangeCheck())
				UpdateButtonTextAndObjectName(m_CharacterProperty.intValue);

			if (m_MatchButtonTextToCharacterProperty.boolValue)
			{
				if (!m_KeyboardButton.textComponent.font.HasCharacter((char)m_CharacterProperty.intValue))
					EditorGUILayout.HelpBox("Character not defined in font, consider using an icon", MessageType.Error);
			}
		}

		// Handle shift character
		m_UseShiftCharacterProperty.boolValue = EditorGUILayout.Toggle("Use Shift Character", m_UseShiftCharacterProperty.boolValue);
		if (m_UseShiftCharacterProperty.boolValue)
		{
			var ch = (char)m_CharacterProperty.intValue;
			if (ch >= kLowercaseStart && ch <= kLowercaseEnd)
			{
				var upperCase = ((char)m_CharacterProperty.intValue).ToString().ToUpper();
				m_ShiftCharIsUppercase = upperCase.Equals(((char)m_ShiftCharacterProperty.intValue).ToString());
				EditorGUI.BeginChangeCheck();
				m_ShiftCharIsUppercase = EditorGUILayout.Toggle("Shift Character is Uppercase", m_ShiftCharIsUppercase);
				if (EditorGUI.EndChangeCheck())
					m_ShiftCharacterProperty.intValue = m_ShiftCharIsUppercase ? upperCase[0] : 0;
			}
			else
				m_ShiftCharIsUppercase = false;

			if (!m_ShiftCharIsUppercase)
				CharacterField("Shift Character", m_ShiftCharacterProperty);
		}
		else
			m_ShiftCharIsUppercase = false;

		EditorGUILayout.PropertyField(m_ButtonMeshProperty);
		EditorGUILayout.PropertyField(m_ButtonGraphicProperty);
		EditorGUILayout.PropertyField(m_RepeatOnHoldProperty);

		serializedObject.ApplyModifiedProperties();
	}

	private void CharacterField(string label, SerializedProperty property)
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUI.BeginChangeCheck();
		var inputString = ((char)property.intValue).ToString();
		inputString = EditorGUILayout.TextField(label, inputString);
		if (EditorGUI.EndChangeCheck())
		{
			property.intValue = (int)GetKeycodeFromString(inputString);
			UpdateButtonTextAndObjectName(property.intValue);
		}

		EditorGUI.BeginChangeCheck();
		property.intValue = (int)(KeyCode)EditorGUILayout.EnumPopup((KeyCode)property.intValue);
		if (EditorGUI.EndChangeCheck())
			UpdateButtonTextAndObjectName(property.intValue);
		EditorGUILayout.EndHorizontal();
	}

	KeyCode GetKeycodeFromString(string inputString)
	{
		if (string.IsNullOrEmpty(inputString))
			return KeyCode.None;

		try
		{
			inputString = Regex.Unescape(inputString);
			return (KeyCode)inputString[0];
		}
		catch (ArgumentException)
		{
			// Incomplete (i.e. user is still typing it out likely) or badly formed unicode string
		}

		return KeyCode.None;
	}

	void UpdateButtonTextAndObjectName(int input)
	{
		var inputString = ((char)input).ToString();

		if (m_MatchButtonTextToCharacterProperty.boolValue)
			m_KeyboardButton.textComponent.text = inputString;

		// For valid keycodes, use the string version of those for 
		if (Enum.IsDefined(typeof(KeyCode), input))
			inputString = ((KeyCode) input).ToString();

		m_KeyboardButton.gameObject.name = inputString;
	}
}