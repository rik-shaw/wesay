using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace WeSay.TreeViewIList.Tests
{
	[TestFixture]
	public class TreeModelIListTests : TreeModelIListAdaptor
	{
		int _PastTheEndIndex;

		public TreeModelIListTests()
			: base(new TreeModelIListConfiguration())
		{
		}

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			this.Configuration.ColumnTypes.Add(GLib.GType.String);
			this.Configuration.GetValueStrategy = delegate(object o, int column)
			{
				Assert.AreEqual(0, column);
				return o;
			};
			List<string> stringList = new List<string>();
			stringList.Add("first item");
			stringList.Add("second item");
			stringList.Add("third item");
			stringList.Add("fourth item");
			this._PastTheEndIndex = stringList.Count;
			this.Configuration.DataSource = stringList;
		}

		[Test]
		public void GetFlags()
		{
			Assert.AreEqual((int)(Gtk.TreeModelFlags.ItersPersist | Gtk.TreeModelFlags.ListOnly),
				GetFlags_callback());
		}

		[Test]
		public void GetNColumns()
		{
			Assert.AreEqual(1, GetNColumns_callback());
		}

		[Test]
		public void GetColumnType()
		{
			Assert.AreEqual(GLib.GType.String.Val, GetColumnType_callback(0));
		}

		[Test]
		public void GetColumnTypeBad()
		{
			Assert.AreEqual(GLib.GType.Invalid.Val, GetColumnType_callback(-1));
			Assert.AreEqual(GLib.GType.Invalid.Val, GetColumnType_callback(1));
		}

		[Test]
		public void GetRow(){
			int index;
			Gtk.TreePath path = new Gtk.TreePath("0");
			Assert.IsTrue(GetRow_callback(out index, path.Handle));
			Assert.AreEqual(0, index);

			int i = this._PastTheEndIndex - 1;
			path = new Gtk.TreePath(i.ToString());
			Assert.IsTrue(GetRow_callback(out index, path.Handle));
			Assert.AreEqual(this._PastTheEndIndex-1, index);
		}


		[Test]
		public void GetRowBad()
		{
			Gtk.TreePath path;
			int index;

			path = new Gtk.TreePath("0.0");
			index = -1;
			Assert.IsFalse(GetRow_callback(out index, path.Handle));
			Assert.AreEqual(this._PastTheEndIndex, index);

			path = new Gtk.TreePath("-1");
			index = -1;
			Assert.IsFalse(GetRow_callback(out index, path.Handle));
			Assert.AreEqual(this._PastTheEndIndex, index);

			path = new Gtk.TreePath("");
			index = -1;
			Assert.IsFalse(GetRow_callback(out index, path.Handle));
			Assert.AreEqual(this._PastTheEndIndex, index);

			path = new Gtk.TreePath(this._PastTheEndIndex.ToString());
			index = -1;
			Assert.IsFalse(GetRow_callback(out index, path.Handle));
			Assert.AreEqual(this._PastTheEndIndex, index);
		}


		[Test]
		public void GetPath()
		{
			Gtk.TreePath path;
			path = new Gtk.TreePath(GetPath_callback(0));
			Assert.AreEqual("0", path.ToString());

			path = new Gtk.TreePath(GetPath_callback(this._PastTheEndIndex-1));
			int i = this._PastTheEndIndex -1;
			Assert.AreEqual(i.ToString(), path.ToString());
		}

		[Test]
		public void GetPathBad()
		{
			Assert.AreEqual(IntPtr.Zero, GetPath_callback(-1));
			Assert.AreEqual(IntPtr.Zero, GetPath_callback(-1));
		}

		[System.Runtime.InteropServices.DllImport("libgobject-2.0-0.dll")]
		static extern void g_value_init(out GLib.Value value, IntPtr type);

		[Test]
		public void GetValue()
		{
			GLib.Value value;
			g_value_init(out value, GLib.GType.String.Val);
			GetValue_callback(0, 0, ref value);
			Assert.AreEqual("first item", value.Val);
		}

		[Test]
		public void Next()
		{
			int index = 0;
			for (int i = 0; i < this._PastTheEndIndex-1; ++i)
			{
				Assert.IsTrue(Next_callback(ref index), "index {0}", index);
				Assert.AreEqual(i + 1, index);
			}
			Assert.IsFalse(Next_callback(ref index));
		}

		[Test]
		public void NextBad()
		{
			int index = -1;
			Assert.IsFalse(Next_callback(ref index));
			Assert.AreEqual(this._PastTheEndIndex, index);

			index = this._PastTheEndIndex;
			Assert.IsFalse(Next_callback(ref index));
			Assert.AreEqual(this._PastTheEndIndex, index);
		}

		[Test]
		public void Children()
		{
			int child = -1;
			Assert.IsTrue(Children_callback(out child, -1));
			Assert.AreEqual(0, child);

			child = -1;
			Assert.IsFalse(Children_callback(out child, 0));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(Children_callback(out child, this._PastTheEndIndex - 1));
			Assert.AreEqual(this._PastTheEndIndex, child);
		}
		[Test]
		public void ChildrenBad()
		{
			int child = -1;
			Assert.IsFalse(Children_callback(out child, -2));
			Assert.AreEqual(this._PastTheEndIndex, child);


			child = -1;
			Assert.IsFalse(Children_callback(out child, this._PastTheEndIndex));
			Assert.AreEqual(this._PastTheEndIndex, child);
		}

		[Test]
		public void HasChild()
		{
			Assert.IsFalse(HasChild_callback(0));
			Assert.IsFalse(HasChild_callback(this._PastTheEndIndex-1));
		}

		[Test]
		public void HasChildBad()
		{
			Assert.IsFalse(HasChild_callback(-1));
			Assert.IsFalse(HasChild_callback(this._PastTheEndIndex));
		}


		[Test]
		public void NChildren()
		{
			Assert.AreEqual(this._PastTheEndIndex, NChildren_callback(-1));
			Assert.AreEqual(0, NChildren_callback(0));
			Assert.AreEqual(0, NChildren_callback(this._PastTheEndIndex - 1));
		}

		[Test]
		public void NChildrenBad()
		{
			Assert.AreEqual(0, NChildren_callback(-2));
			Assert.AreEqual(0, NChildren_callback(this._PastTheEndIndex));
		}

		[Test]
		public void NthChild()
		{
			int child = -1;
			Assert.IsTrue(NthChild_callback(out child, -1, 0));
			Assert.AreEqual(0, child);

			child = -1;
			Assert.IsTrue(NthChild_callback(out child, -1, this._PastTheEndIndex - 1));
			Assert.AreEqual(this._PastTheEndIndex-1, child);
		}

		[Test]
		public void NthChildBad()
		{
			int child = -1;
			Assert.IsFalse(NthChild_callback(out child, -1, -1));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, -1, this._PastTheEndIndex));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, -2, -1));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, -2, 0));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, -2, 1));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, -2, -1));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, 0, 0));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, 0, 1));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, this._PastTheEndIndex - 1, -1));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, this._PastTheEndIndex - 1, 0));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, this._PastTheEndIndex - 1, 1));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, this._PastTheEndIndex, -1));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, this._PastTheEndIndex, 0));
			Assert.AreEqual(this._PastTheEndIndex, child);

			child = -1;
			Assert.IsFalse(NthChild_callback(out child, this._PastTheEndIndex, 1));
			Assert.AreEqual(this._PastTheEndIndex, child);
		}

		[Test]
		public void Parent()
		{
			int parent = -1;
			Assert.IsFalse(Parent_callback(out parent, 0));
			Assert.AreEqual(this._PastTheEndIndex, parent);

			parent = -1;
			Assert.IsFalse(Parent_callback(out parent, this._PastTheEndIndex - 1));
			Assert.AreEqual(this._PastTheEndIndex, parent);
		}
		[Test]
		public void ParentBad()
		{
			int parent = -1;
			Assert.IsFalse(Parent_callback(out parent, -1));
			Assert.AreEqual(this._PastTheEndIndex, parent);

			parent = -1;
			Assert.IsFalse(Parent_callback(out parent, this._PastTheEndIndex));
			Assert.AreEqual(this._PastTheEndIndex, parent);
		}
	}
}
