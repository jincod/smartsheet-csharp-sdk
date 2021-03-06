﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Smartsheet.Api.Models
{
	/// <summary>
	/// Represents the widget object. </summary>
	/// <seealso href="http://smartsheet-platform.github.io/api-docs/#shortcutdataitem-object">ShortDataItem Object Help</seealso>
	public class ShortcutDataItem
	{
		/// <summary>
		/// Label for the data point
		/// </summary>
		private string label;

		/// <summary>
		/// formatDescriptor
		/// </summary>
		private string labelFormat;

		/// <summary>
		/// Attachment type (one of FILE, GOOGLE_DRIVE, LINK, BOX_COM, DROPBOX, EVERNOTE, or EGNYTE)
		/// </summary>
		private string mimeType;

		/// <summary>
		/// Hyperlink  object
		/// </summary>
		private Link hyperlink;

		/// <summary>
		/// The display order for the ShortcutWidgetItem
		/// </summary>
		private int? order;

		/// <summary>
		/// Label for the data point. 
		/// </summary>
		/// <returns> the label </returns>
		public virtual string Label
		{
			get
			{
				return label;
			}
			set
			{
				this.label = value;
			}
		}

		/// <summary>
		/// formatDescriptor.
		/// </summary>
		/// <returns> the labelFormat </returns>
		public virtual string LabelFormat
		{
			get
			{
				return labelFormat;
			}
			set
			{
				this.labelFormat = value;
			}
		}

		/// <summary>
		/// Attachment type (one of FILE, GOOGLE_DRIVE, LINK, BOX_COM, DROPBOX, EVERNOTE, or EGNYTE).
		/// </summary>
		/// <returns> the MIME type </returns>
		public virtual string MimeType
		{
			get
			{
				return mimeType;
			}
			set
			{
				this.mimeType = value;
			}
		}

		/// <summary>
		/// Hyperlink object.
		/// </summary>
		/// <returns> the Link </returns>
		public virtual Link Hyperlink
		{
			get
			{
				return hyperlink;
			}
			set
			{
				this.hyperlink = value;
			}
		}
		
		/// <summary>
		/// The display order for the CellDataItem.
		/// </summary>
		/// <returns> the display order </returns>
		public virtual int? Order
		{
			get
			{
				return order;
			}
			set
			{
				this.order = value;
			}
		}
	}
}
