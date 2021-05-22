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
		public TextElement TitleElement { get; private set; }

		public Pane() : this("Title")
		{
		}

		public Pane(string title)
		{
			TitleElement = new TextElement();
			TitleElement.AddToClassList("pane__title");
			TitleElement.text = title;
			this.Add(TitleElement);
			this.AddToClassList("pane");
		}

		public string Title
		{
			get => TitleElement.text;
			set => TitleElement.text = value;
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
				((Pane)ve).TitleElement.text = _title.GetValueFromBag(bag, cc);
			}
		}
	}
}