(function ()
{
	// create tzShortcodes plugin
	tinymce.create("tinymce.plugins.tzShortcodes",
	{
		init: function ( ed, url )
		{
			ed.addCommand("tzPopup", function ( a, params )
			{
				var popup = params.identifier;
				
				// load thickbox
				tb_show("Insert Shortcode", url + "/popup.php?popup=" + popup + "&width=" + 800);
			});
		},
		createControl: function ( btn, e )
		{
			if ( btn == "tz_button" )
			{	
				var a = this;
					
				// adds the tinymce button
				btn = e.createMenuButton("tz_button",
				{
					title: "Insert Shortcode",
					image: "../wp-content/themes/advocate/tinymce/images/icon.png",
					icons: false
				});
				
				// adds the dropdown to the button
				btn.onRenderMenu.add(function (c, b)
				{					
					a.addWithPopup( b, "Columns", "columns" );
					a.addWithPopup( b, "App Store Buttons", "app_store_button" );
					a.addWithPopup( b, "Buttons", "button" );
					a.addWithPopup( b, "Toggle Content", "toggle" );
					a.addWithPopup( b, "Tabbed Content", "tabs" );
					a.addImmediate( b, "Update List", '[update_list]<br/>&nbsp;&nbsp;[update_item title="NEW" color="blue"] Update item... [/update_item]<br/>&nbsp;&nbsp;[update_item title="FIXED" color="green"] Update item... [/update_item]<br/>&nbsp;&nbsp;[update_item title="IMPROVED" color="teal"] Update item... [/update_item]<br/>&nbsp;&nbsp;[update_item title="REMOVED" color="red"] Update item... [/update_item]<br/>&nbsp;&nbsp;[update_item title="UPDATED" color="gray"] Update item... [/update_item]<br/>[/update_list]' );
					a.addImmediate( b, "Team List", '[team] <br/>&nbsp;&nbsp;[team_member name="Jane Doe" photo="..." title="Designer" url="http://website.com" url_title="website.com"] <br/>&nbsp;&nbsp;&nbsp;&nbsp;[social_links]<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[social_link site="twitter"]http://twitter.com/two2twelve[/social_link]<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[social_link site="skype"]#[/social_link]<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[social_link site="dribbble"]#[/social_link]<br/>&nbsp;&nbsp;&nbsp;&nbsp;[/social_links] <br/>&nbsp;&nbsp;[/team_member] <br/><br/>&nbsp;&nbsp;[team_member name="John Smith" photo="..." title="Developer" url="http://website.com" url_title="website.com"]<br/>&nbsp;&nbsp;&nbsp;&nbsp;[social_links]<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[social_link site="twitter"]http://twitter.com/two2twelve[/social_link]<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[social_link site="skype"]#[/social_link]<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[social_link site="facebook"]#[/social_link]<br/>&nbsp;&nbsp;&nbsp;&nbsp;[/social_links] <br/>&nbsp;&nbsp;[/team_member] <br/>[/team]' );
				});
				
				return btn;
			}
			
			return null;
		},
		addWithPopup: function ( ed, title, id ) {
			ed.add({
				title: title,
				onclick: function () {
					tinyMCE.activeEditor.execCommand("tzPopup", false, {
						title: title,
						identifier: id
					})
				}
			})
		},
		addImmediate: function ( ed, title, sc) {
			ed.add({
				title: title,
				onclick: function () {
					tinyMCE.activeEditor.execCommand( "mceInsertContent", false, sc )
				}
			})
		},
		getInfo: function () {
			return {
				longname: 'TZ Shortcodes',
				author: 'Orman Clark',
				authorurl: 'http://themeforest.net/user/ormanclark/',
				infourl: 'http://wiki.moxiecode.com/',
				version: "1.0"
			}
		}
	});
	
	// add tzShortcodes plugin
	tinymce.PluginManager.add("tzShortcodes", tinymce.plugins.tzShortcodes);
})();