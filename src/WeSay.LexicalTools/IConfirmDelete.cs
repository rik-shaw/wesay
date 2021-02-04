﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeSay.LexicalTools
{
	public delegate IConfirmDelete ConfirmDeleteFactory();

	public interface IConfirmDelete
	{
		bool DeleteConfirmed { get; }
		string Message { get; set; }
	}
}
