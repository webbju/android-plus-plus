/**
 * Appendo Plugin for jQuery v1.01
 * Creates interface to create duplicate clones of last table row (usually for forms)
 * (c) 2008 Kelly Hallman. Free software released under MIT License.
 * See http://deepliquid.com/content/Appendo.html for more info
 */

// Attach appendo as a jQuery plugin
jQuery.fn.appendo = function(opt)
{
	this.each(function() { jQuery.appendo.init(this,opt); });
	return this;
};

// appendo namespace
jQuery.appendo = function() {

	// Create a closure so that we can refer to "this" correctly down the line
	var myself = this;

	// Global Options
	// These can be set with inline Javascript like so:
	// jQuery.appendo.opt.maxRows = 5;
	// $.appendo.opt.allowDelete = false;
	// (no need, in fact you shouldn't, wrap in jQuery(document).ready() etc)
	this.opt = { };

	this.init = function(obj,opt) {

		// Extend the defaults with global options and options given, if any
		var options = jQuery.extend({
				labelAdd:		'Add Row',
				labelDel:		'Remove',
				allowDelete:	true,
				// copyHandlers does not seem to work
				// it's been removed from the docs for now...
				copyHandlers:	false,
				focusFirst:		true,
				onAdd:			function() { return true; },
				onDel:		function() { return true; },
				maxRows:		0,
				wrapClass:		'appendoButtons',
				wrapStyle:		{ padding: '.4em .2em .5em' },
				buttonStyle:	{ marginRight: '.5em' },
				subSelect:		'tr:last'
			},
			myself.opt,
			opt
		);

		// Store clone of last table row
		var $cpy = jQuery(obj).find(options.subSelect).clone(options.copyHandlers);
		// We consider this starting off with 1 row
		var rows = 1;
		// Create two button objects
		var $add_btn = jQuery('#form-child-add').click(clicked_add),
			$del_btn = new_button(options.labelDel).click(clicked_del).hide()
		;

		// Append a row to table instance
		function add_row()
		{
			var $dup = $cpy.clone(options.copyHandlers);
			$dup.appendTo(obj);
			update_buttons(1);
			if (typeof(options.onAdd) == "function") options.onAdd($dup);
			if (!!options.focusFirst) $dup.find('input:first').focus();
		};

		// Remove last row from table instance
		function del_row()
		{
			var $row = jQuery(obj).find(options.subSelect);
			if ((typeof(options.onDel) != "function") || options.onDel($row))
			{
				$row.remove();
				update_buttons(-1);
			}
		};

		// Updates the button states after rows change
		function update_buttons(rowdelta)
		{
			// Update rows if a delta is provided
			rows = rows + (rowdelta || 0);
			// Disable the add button if maxRows is set and we have that many rows
			// $add_btn.attr('disabled',(!options.maxRows || (rows < options.maxRows))?false:true);
			// Show remove button if we've added rows and allowDelete is set
			(options.allowDelete && (rows > 1))? $del_btn.show(): $del_btn.hide();
		};

		// Returns (jQuery) button objects with label
		function new_button(label)
		{
			return jQuery('<button />')
				.css(options.buttonStyle)
				.html(label);
		};

		// This function can be returned to kill a received event
		function nothing(e)
		{
			e.stopPropagation();
			e.preventDefault();
			return false;
		};

		// Handles a click on the add button
		function clicked_add(e)
		{
			if (!options.maxRows || (rows < options.maxRows)) add_row();
			return nothing(e);
		};

		// Handles a click event on the remove button
		function clicked_del(e)
		{
			if (rows > 1) del_row(); 
			return nothing(e);
		};
	
		// Add the buttons after the table instance
		/*
		jQuery('<div />')
			.addClass(options.wrapClass)
			.css(options.wrapStyle)
			.append( $add_btn, $del_btn )
			.insertAfter(obj);
		*/

		// Update the buttons
		update_buttons();

	};
	return this;
}();