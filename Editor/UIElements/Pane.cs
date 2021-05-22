using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[assembly: UxmlNamespacePrefix("ZeludeEditor", "zeludeEditor")]
namespace ZeludeEditor
{
	public class Pane : VisualElement
	{
		private TextElement _titleElement;

		public string Title
		{
			get => _titleElement.text;
			set => _titleElement.text = value;
		}

		public Pane() : this("Title")
		{
		}

		public Pane(string title)
		{
			_titleElement = new TextElement();
			_titleElement.AddToClassList("pane__title");
			_titleElement.text = title;
			this.Add(_titleElement);
			this.AddToClassList("pane");
		}

		public new class UxmlFactory : UxmlFactory<Pane, UxmlTraits> { }

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription _title = new UxmlStringAttributeDescription { name = "title" };

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((Pane)ve)._titleElement.text = _title.GetValueFromBag(bag, cc);
			}
		}
	}
}