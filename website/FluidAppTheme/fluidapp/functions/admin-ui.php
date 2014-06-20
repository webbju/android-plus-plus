<?php

function t2t_get_page_id($name) {
	global $wpdb;
	$page_id = $wpdb->get_var("SELECT ID FROM $wpdb->posts WHERE ( post_name = '".$name."' or post_title = '".$name."' ) and post_status = 'publish' and post_type='page' ");
	return $page_id;
}

function t2t_get_page_permalink($name) {
	$page_id = t2t_get_page_id($name);
	return get_permalink($page_id);
}

function t2t_admin() {

global $themename, $themeversion, $shortname, $options;
$i=0;

if (isset($_REQUEST['saved'])) echo '<div id="flash_message" class="updated fade"><p><strong>'.$themename.' settings saved.</strong></p></div>';
if (isset($_REQUEST['reset'])) echo '<div id="flash_message" class="updated fade"><p><strong>'.$themename.' settings reset.</strong></p></div>';

?>

<script type="text/javascript" charset="utf-8">

	function set_ajax_upload(clickedObject, clickedID) {
		new AjaxUpload(clickedID, {
			  action: '<?php echo admin_url("admin-ajax.php"); ?>',
			  name: clickedID, // File upload name
			  data: { // Additional data to send
					action: 'tz_ajax_post_action',
					type: 'upload',
					data: clickedID },
			  autoSubmit: true, // Submit file after selection
			  responseType: false,
			  onChange: function(file, extension){},
			  onSubmit: function(file, extension){
					clickedObject.text('Uploading'); // change button text, when user selects file	
					this.disable(); // If you want to allow uploading only 1 file at time, you can disable upload button
					interval = window.setInterval(function(){
						var text = clickedObject.text();
						if (text.length < 13){	clickedObject.text(text + '.'); }
						else { clickedObject.text('Uploading'); } 
					}, 200);
			  },
			  onComplete: function(file, response) {
			   
				window.clearInterval(interval);
				clickedObject.text('Upload Image');	
				this.enable(); // enable upload button
				
				// If there was an error
				if(response.search('Upload Error') > -1){
					var buildReturn = '<span class="upload-error">' + response + '</span>';
					jQuery(".upload-error").remove();
					clickedObject.parent().after(buildReturn);
				
				}
				else{
					var buildReturn = '<img class="t2t-option-image" id="image_'+clickedID+'" src="'+response+'" alt="" />';

					jQuery(".upload-error").remove();
					jQuery("#image_" + clickedID).remove();	
					clickedObject.parent().parent().find('span.upload_hint').before(buildReturn);
					clickedObject.next('span').fadeIn();
					clickedObject.parent().prev('input').val(response);
				}
			  }
			});
	}

	jQuery(document).ready(function() {
		//AJAX Upload
		jQuery('.image_upload_button').each(function(){
		
			var clickedObject = jQuery(this);
			var clickedID = jQuery(this).attr('id');	
			set_ajax_upload(clickedObject, clickedID);
		
		});
		
		//AJAX Remove (clear option value)
		jQuery('.image_reset_button').live('click',function(){
		
				var clickedObject = jQuery(this);
				var clickedID = jQuery(this).attr('id');
				var theID = jQuery(this).attr('title');	
				
				if(jQuery(this).hasClass('screenshot_reset')) {
					var save_type = 'screenshot_reset'
				} else {
					var save_type = 'image_reset'
				}

				var ajax_url = '<?php echo admin_url("admin-ajax.php"); ?>';
			
				var data = {
					action: 'tz_ajax_post_action',
					type: save_type,
					data: theID
				};

				jQuery.post(ajax_url, data, function(response) {
					var image_to_remove = jQuery('#image_' + theID);
					var button_to_hide = jQuery('#reset_' + theID);
					image_to_remove.fadeOut(500,function(){ jQuery(this).remove(); });
					button_to_hide.fadeOut();
					clickedObject.parent().prev('input').val('');					
				});
				
				return false; 
				
			});
			
			jQuery('.t2t-radio-img-img').click(function(){
				jQuery(this).parent().parent().find('.t2t-radio-img-img').removeClass('t2t-radio-img-selected');
				jQuery(this).addClass('t2t-radio-img-selected');
				
			});
			jQuery('.t2t-radio-img-label').hide();
			jQuery('.t2t-radio-img-img').show();
			jQuery('.t2t-radio-img-radio').hide();
	});
</script>

<div id="t2t_container" class="wrap">
	<form action="" method="post" enctype="multipart/form-data" >
	<div id="header">
     	<div class="logo">
        	<h2><?php echo $themename; ?></h2>
      	</div>
		<input type="submit" class="button-primary t2t-button" name="save<?php echo $i; ?>" value="Save All Changes">
    </div>
	<div id="main">
		<div id="t2t-nav">
	        <ul>
	        	<?php foreach ($options as $value) { 
					  if($value['type'] == "section") {	
				?>
				<li><a href="#<?php echo str_replace(" ","_", strtolower($value['name'])); ?>"><?php echo $value['name']; ?></a></li>
				<?php } }?>  
			</ul>
		</div>
		
		<div id="content">
			
<?php foreach ($options as $value) {
switch ( $value['type'] ) {

case "open":

?>
	<div class="form-table t2t-form">
<?php break;
case "close":
?>

		</div>
	  </div>
	</div>
<?php break;
case 'sidebar':
?>
	<div class="row">
		<br/>
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<input size="<?php echo $value['size']; ?>" name="<?php echo $value['id']; ?>" id="<?php echo $value['id']; ?>" type="text" value="<?php echo $value['std']; ?>" />
			<br /><span class="hint"><?php echo $value['desc']; ?></span>
		</div>
	</div>
<?php 

break;
case 'sidebar_delete':
?>
<?php
	$get_sidebar_options = sidebar_generator_t2t::get_sidebars();
	if($get_sidebar_options != "") {
	$i=1;

	foreach ($get_sidebar_options as $sidebar_gen) { ?>
	<?php if($i == 1) { ?>
		<div class="row">
			<label></label>
			<div class="content">
				<div align="left">
					<h4 style="margin-bottom: 0px; font-size: 14px; border-bottom: 1px solid #ddd; padding-bottom: 5px"><?php echo $value['desc']; ?></h4>
				</div>
			</div>
		</div>
	<?php } ?>
		<div id="sidebar_table_<?php echo $i; ?>" class="row">
			<label></label>
			<div class="content sidebar_row">
				<div align="left" style="float:left;font-size:13px;overflow:hidden; padding-top: 5px"><?php echo $sidebar_gen; ?></div>
				<a href="javascript:;" id="<?php echo $i; ?>" class="delete_sidebar">Delete</a>
				<div style="margin:3px 0 0 8px;float:left;"><img class="sidebar_rm_<?php echo $i; ?>" style="display:none;" src="images/wpspin_light.gif" alt="Loading" /></div>
				<input id="<?php echo 'sidebar_generator_'.$i ?>" type="hidden" name="<?php echo 'sidebar_generator_'.$i ?>" value="<?php echo $sidebar_gen; ?>" />
			</div>
		</div>
	<?php $i++;  
	} 
	}
break;
case 'text':
	$val = $value['std'];
	$std = get_option($value['id']);
	if ( $std != "") { $val = htmlspecialchars($std, ENT_QUOTES); }
?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<input name="<?php echo $value['id']; ?>" id="<?php echo $value['id']; ?>" type="<?php echo $value['type']; ?>" value='<?php echo $val; ?>' style="width: <?php if(isset($value['width'])) { echo $value['width']; } ?>px;" />
			<br/>
		 	<span class="hint"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>
		</div>
	</div>
<?php
break;
case 'textarea':
?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content"><textarea name="<?php echo $value['id']; ?>" type="<?php echo $value['type']; ?>" cols="" rows="<?php if(isset($value['height'])) { echo $value['height']; } ?>"><?php if ( get_option( $value['id'] ) != "") { echo stripslashes(get_option( $value['id']) ); } else { echo $value['std']; } ?></textarea>
		<br/>
		<span class="hint"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>
		</div>
	</div>
<?php
break;
case 'editor':
?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<?php if ( get_option( $value['id'] ) != "") { $content = stripslashes(get_option( $value['id']) ); } else { $content = $value['std']; } ?>
			<div id="<?php echo user_can_richedit() ? 'postdivrich' : 'postdiv'; ?>" class="postarea">
				<?php the_editor(stripslashes($content), $value['id'], $value['id'], true); ?>
			</div>
			<span class="hint"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>
		</div>
	</div>

<?php
break;
case 'select':
?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<select name="<?php echo $value['id']; ?>" id="<?php echo $value['id']; ?>" class="<?php if(isset($value['class'])) { echo $value['class']; } ?>">
			<!--<option value="">Select</option>-->
			<?php foreach ($value['options'] as $option) { ?>
					<option <?php if (get_option( $value['id'] ) == $option) { echo 'selected="selected"'; } ?>><?php echo $option; ?></option>		
			<?php } ?>
			</select>
			<br/>
			<span class="hint"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>
		</div>
	</div>
<?php
break;
case 'google_fonts':

$font_list = t2t_get_google_fonts();

?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<select name="<?php echo $value['id']; ?>" id="<?php echo str_replace(" ","_", strtolower($value['name'])); ?>" class="tyopgraphy_font_trigger" style="width: 150px; float: left; margin-right: 10px;">
			<option value="">Select</option>
			<?php foreach ( $font_list as $font ) { ?>					
					<option value="<?php echo $font['family']; ?>"<?php if(get_option( $value['id'] ) == "" && $font['family'] == $value['std']) { echo ' selected';  } elseif ( get_option( $value['id'] ) === $font['family'] ) { echo ' selected'; } ?>><?php echo $font['family'] ; ?></option>	
			<?php } ?>
			</select>
			
			<select class="tyopgraphy_font_variants" style="width: 78px; float: left; margin-right: 10px;"></select>
			
			<script type="text/javascript" charset="utf-8">
				jQuery(document).ready(function() {
					<?php
						if(get_option( $value['id'] ) == "") { 
							$font = $value['std'];
						} else { 
							$font = get_option( $value['id'] );
						}
						if(get_option( $value['id'].'_variant' ) == "") { 
							$font_variant = $value['font_variant'];
						} else { 
							$font_variant = get_option( $value['id'].'_variant' );
						}
					?>
					getFontVariants(jQuery("#<?php echo str_replace(" ","_", strtolower($value['name'])); ?>"), "<?php echo $font_variant; ?>");
				});
			</script>		
			
			<select class="tyopgraphy_size_trigger" style="width: 60px; float: left; margin-right: 10px;">
				<option value="">Default</option>
				<?php
					$number = 10;
					while ($number < 61) {
						
						if(get_option( $value['id'].'_size') == "" && $number == $value['font_size']) {
							$selected = 'selected';
						} elseif(get_option( $value['id'].'_size') == $number) { 
							$selected = 'selected'; 
						} else {
							$selected = "";
						}
						
						 //if(get_option( $value['id'].'_size') == $number) { $selected = 'selected'; } else { $selected = ""; }
					     echo '<option value="'.$number.'px" '.$selected.'>'.$number.'px</option>';
					     $number += 1;
					}
				?>
			</select>
			
			<div class="colorSelector"><div style="background-color: <?php if ( get_option( $value['id'].'_color') != "") { echo stripslashes(get_option( $value['id'].'_color')  ); } else { echo $value['font_color']; } ?>;"></div></div>
			<input name="<?php echo $value['id']; ?>_color" class="tyopgraphy_color_trigger" type="text" value='<?php if ( get_option( $value['id'].'_color') != "") { echo stripslashes(get_option( $value['id'].'_color')  ); } else { echo $value['font_color']; } ?>' style="width: 80px;" />
			
			<br/>
			<span class="hint"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>
			
			<?php 
				if(get_option($value['id']) == "") {
					$font = $value['std'];
				} else {
					$font = get_option($value['id']); 
				}				
			?>
									
			<div class="typography_preview">
				<div style="font-family: <?php echo "'$font'"; ?>; font-size: <?php if(get_option( $value['id'].'_size') != "") { echo get_option($value['id'].'_size'); } else { echo $value['font_size'].'px'; } ?>; color: <?php if(get_option( $value['id'].'_color') != "") { echo get_option($value['id'].'_color'); } else { echo "#333333"; } ?>;  font-weight: <?php if(get_option( $value['id'].'_variant') != "") { echo get_option($value['id'].'_variant'); } else { echo ""; } ?>;"><?php echo $value['name']; ?></div>
			</div>
			
			<a href="javascript:;" class="revert" data-font_family="<?php echo $value['std']; ?>" data-font_size="<?php echo $value['font_size']; ?>px" data-font_color="<?php echo $value['font_color']; ?>" data-font_variant="<?php echo $value['font_variant']; ?>"><span class="icon">&#10226;</span> Revert to default</a>
			
			<script type="text/javascript" charset="utf-8">
				jQuery(document).ready(function() {
					var stylesheet_id = '<?php echo str_replace(" ","_", strtolower($value['name'])); ?>-font';
					<?php if(get_option($value['id']) != "") { ?>
						var font = '<?php echo $font; ?>';
					<?php } else { ?>
						var font = '<?php echo $value['std']; ?>';
					<?php } ?>
					
					<?php if(get_option($value['id'].'_variant') != "") { ?>
						var variant = ':<?php echo get_option($value['id'].'_variant'); ?>';
					<?php } else { ?>
						var variant = ':<?php echo $value['font_variant']; ?>';
					<?php } ?>
					
					jQuery('head').append('<link class="'+stylesheet_id+'" rel="stylesheet" type="text/css" href="http://fonts.googleapis.com/css?family='+font+variant+'" />');
					
					<?php if($value['id'] == "t2t_logo_font" && get_option($shortname.'_logo_type') != "text") { ?>
						jQuery("div.t2t-form div.logo").css({ 'opacity' : '0.5' }).find("input,select").attr("disabled", "disabled").end().click(function() {
							alert('Please select the "Use Text" option for your logo under "General Settings" before modifying the logo typography.');
							return false;
						});
					<?php } ?>
					
					jQuery("a.revert").click(function() {
						jQuery(this).parents(".content")
							.find(".tyopgraphy_font_trigger")
							.find("option[value='"+jQuery(this).attr("data-font_family")+"']")
							.attr("selected", "selected").end()
							.trigger("change");
							
						jQuery(this).parents(".content")
							.find(".tyopgraphy_font_variants")
							.find("option[value='"+jQuery(this).attr("data-font_variant")+"']")
							.attr("selected", "selected").end()
							.trigger("change");
							
						jQuery(this).parents(".content")
							.find(".tyopgraphy_size_trigger")
							.find("option[value='"+jQuery(this).attr("data-font_size")+"']")
							.attr("selected", "selected").end()
							.trigger("change");
							
						jQuery(this).parents(".content")
							.find(".tyopgraphy_color_trigger, .typography_color")
							.val(jQuery(this).attr("data-font_color"))
							.end()
							.find(".colorSelector div")
							.css("background-color", jQuery(this).attr("data-font_color"))
							.end()
							.find(".typography_preview div")
							.css('color', jQuery(this).attr("data-font_color"));
						jQuery("#t2t_"+jQuery(this).parent().parent().parent().attr("class")+"_font_color").val(jQuery(this).attr("data-font_color"));
					});
					
				});
			</script>
			
		</div>
	</div>
<?php
break;
case 'select_taxonomy':
?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<?php 
				wp_dropdown_categories(array(
					'hide_empty' => 0, 
					'show_option_none' => $value['std'],
					'name' => $value['id'],
					'selected' => get_option($value['id']),
					'taxonomy' => $value['taxonomy']
				)); 
			?> 
			<span class="hint"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>
		</div>
	</div>
<?php
break;
case 'images':
$i = 0;
$select_value = get_option( $value['id']);
?>

	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<?php
				foreach ($value['options'] as $key => $option) 
				 { 
				 $i++;

					 $checked = '';
					 $selected = '';
					   if($select_value != '') {
							if ( $select_value == $key) { $checked = ' checked'; $selected = 't2t-radio-img-selected'; } 
					    } else {
							if ($value['std'] == $key) { $checked = ' checked'; $selected = 't2t-radio-img-selected'; }
							elseif ($i == 1  && !isset($select_value)) { $checked = ' checked'; $selected = 't2t-radio-img-selected'; }
							elseif ($i == 1  && $value['std'] == '') { $checked = ' checked'; $selected = 't2t-radio-img-selected'; }
							else { $checked = ''; }
						}
				
					$output = '<span>';
					$output .= '<input type="radio" id="t2t-radio-img-' . $value['id'] . $i . '" class="checkbox t2t-radio-img-radio" value="'.$key.'" name="'. $value['id'].'" '.$checked.' />';
					$output .= '<div class="t2t-radio-img-label">'. $key .'</div>';
					$output .= '<img src="'.$option.'" alt="" class="t2t-radio-img-img '. $selected .'" onClick="document.getElementById(\'t2t-radio-img-'. $value['id'] . $i.'\').checked = true;" />';
					$output .= '</span>';
					
					echo $output;
				}
			?>
			<div class="clear"></div>
			<span class="hint"><?php echo $value['desc']; ?></span>
		</div> 
	</div>

<?php
break;
case 'colorpicker':
?>

	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<div id="<?php echo $value['id']; ?>_picker" class="colorSelector"><div style="background-color: <?php if ( get_option( $value['id'] ) != "") { echo stripslashes(get_option( $value['id'])  ); } else { echo $value['std']; } ?>;"></div></div>
			<input name="<?php echo $value['id']; ?>" id="<?php echo $value['id']; ?>" type="<?php echo $value['type']; ?>" value='<?php if ( get_option( $value['id'] ) != "") { echo stripslashes(get_option( $value['id'])  ); } else { echo $value['std']; } ?>' style="width: <?php echo $value['width']; ?>px;" />
			<br/>
		 	<span class="hint"><?php echo $value['desc']; ?></span>
		</div>
	</div>
<?php
break;
case "checkbox":
?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content check">
			<?php if(get_option($value['id'])){ $checked = "checked=\"checked\""; }else{ $checked = "";} ?>
			<input type="checkbox" class="checkbox" name="<?php echo $value['id']; ?>" id="<?php echo $value['id']; ?>" value="true" <?php echo $checked; ?> />
			<span class="hint"><?php echo $value['desc']; ?></span>
		</div>
	</div>

<?php break;
case "fb_setup":
?>
	<?php if(get_option($shortname.'_facebook_page') == "Select a page" || get_option($shortname.'_facebook_page') == "") { ?>
	<?php } else { ?>
	<hr/><br/>
		<div class="row">
			<label>Setup Instructions</label>
			<div class="clear"></div>
			<div class="step">
				<span>1</span>
				<div class="instructions"><p>Login to your Facebook account and add the <a href="http://www.facebook.com/apps/application.php?id=190322544333196" target="_blank">Static HTML: iFrame Tabs</a> application to your Facebook page.</p></div>
			</div>
			<div class="step">
				<span>2</span>
				<div class="instructions">
					<p>Once added, click the <b>"Welcome"</b> icon on your Facebook page's sidebar and paste the following code into the box labeled <b>"Enter your content here"</b>:</p>
					<textarea>
<html>
  <head>
  <meta http-equiv='Refresh'
        content='0; url=<?php echo t2t_get_page_permalink(get_option($shortname.'_facebook_page')); ?>' />
  </head>
</html></textarea>
				</div>
			</div>
			<div class="step">
				<span>3</span>
				<div class="instructions"><p>Check the box labeled <b>"No scrollbars"</b> then click <b>"Save and view tab..."</b>.</p></div>
			</div>
		</div>
	<?php } ?>
<?php break;
case "separator":
?>
	<div class="row">
		<hr />
	</div>
<?php break;
case "group_open":
?>
	<div class="field_group">
<?php break;
case "sub_group_open":
?>
	<div class="sub_group">
<?php break;
case "group_close":
?>
	</div>
<?php break;
case "sub_group_close":
?>
	</div>
<?php break;
case "desc":
?>
	<div class="row">
		<p class="desc"><?php echo $value['desc']; ?></p>
	</div>
<?php break;
case "upload":
	$upload = get_option( $value['id'] );
 ?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<input class="tz-input" name="<?php echo $value['id'] ?>" id="<?php echo $value['id'] ?>_upload" type="text" value="<?php echo $upload; ?>" />
			<div class="upload_button_div">
				<span class="button image_upload_button" id="<?php echo $value['id'] ?>">Upload Image</span>
				<?php if(!empty($upload)) {$hide = '';} else { $hide = 'hide';} ?>
				<span class="button image_reset_button <?php echo $hide; ?>" id="reset_<?php echo $value['id'] ?>" title="<?php echo $value['id'] ?>">Remove</span>
				<?php if(!empty($upload)) { ?>
				<img class="tz-option-image" id="image_<?php echo $value['id'] ?>" src="<?php echo $upload; ?>" alt="" />
				<?php } ?>
			</div>
			<span class="hint upload_hint"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>
		</div>
	</div>
<?php

break;
case "select_page":
$page_list = get_pages();
 ?>
	
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<select name="<?php echo $value['id']; ?>" id="<?php echo $value['id']; ?>">
			<option value=""><?php echo $value['std']; ?></option>
			<?php foreach ($page_list as $option) { ?>
					<option value="<?php echo $option->ID ?>" <?php if (get_option( $value['id'] ) == $option->ID) { echo 'selected="selected"'; } ?>><?php echo $option->post_title; ?></option>		
			<?php } ?>
			</select>
			<br/>
			<span class="hint"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>	
				
		</div>
	</div>
	
<?php

break;
case "logo":
	$upload = get_option( $value['id'] );
 ?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content">
			<div class="field_group" style="padding-bottom: 5px;">
				
				<div class="logo_type">
					<input type="radio" class="radio logo_type_radio" name="<?php echo $shortname.'_logo_type'; ?>" value="upload" <?php if(get_option($shortname.'_logo_type') == "upload"){ echo 'checked="checked"'; } ?> /> <span class="label">Use an Image</span>
					&nbsp;&nbsp;&nbsp;&nbsp;
					<input type="radio" class="radio logo_type_radio" name="<?php echo $shortname.'_logo_type'; ?>" value="text" <?php if(get_option($shortname.'_logo_type') == "text"){ echo 'checked="checked"'; } ?> /> <span class="label">Use Text</span>
					
					<div class="upload">
						<span class="hint">Upload or type the full URL of your logo image.</span>
						<input class="tz-input" name="<?php echo $value['id'] ?>" id="<?php echo $value['id'] ?>_upload" type="text" value="<?php echo $upload; ?>" />
						<div class="upload_button_div">
							<span class="button image_upload_button" id="<?php echo $value['id'] ?>">Upload Image</span>
							<?php if(!empty($upload)) {$hide = '';} else { $hide = 'hide';} ?>
							<span class="button image_reset_button <?php echo $hide; ?>" id="reset_<?php echo $value['id'] ?>" title="<?php echo $value['id'] ?>">Remove</span>
							<?php if(!empty($upload)) { ?>
							<img class="tz-option-image" id="image_<?php echo $value['id'] ?>" src="<?php echo $upload; ?>" alt="" />
							<?php } ?>
						</div>
					</div>
					<div class="text">
						<span class="hint">Use plain text instead of an image for your logo.</span>
						<input class="logo_fields" name="<?php echo $shortname.'_logo_text'; ?>" id="<?php echo $shortname.'_logo_text_trigger'; ?>" type="text" value='<?php if ( get_option($shortname.'_logo_text') != "") { echo stripslashes(get_option($shortname.'_logo_text')  ); } else { echo $value['std']; } ?>' style="width: 405px; float: left; margin-right: 10px;" />
					</div>
				</div>

			</div>
		</div>
	</div>
<?php

break;
case 'radio':
?>
	<div class="row">
		<label><?php echo $value['name']; ?></label>
		<div class="content check">	
			<?php
			$check_name = $value['id'];
			$this_value = (get_option($value['id'])) ? get_option($value['id']) : $value['std'];
			foreach ($value['desc'] as $desc => $value) { ?>

			<input name="<?php echo $check_name; ?>" id="<?php echo $check_name; ?>" class="radio" type="radio" value="<?php echo $value; ?>" <?php if ($this_value == $value){echo 'checked="checked"';}?> /> <?php echo $desc; ?><br /> 

			<?php } ?>
		</div>
	</div>
<?php

break;
case 'hidden':
?>
<input name="<?php echo $value['id']; ?>" id="<?php echo $value['id']; ?>" type="<?php echo $value['type']; ?>" class="<?php if(isset($value['class'])) { echo $value['class']; } else {} ?>" value='<?php if ( get_option( $value['id'] ) != "") { echo stripslashes(get_option( $value['id'])  ); } else { echo $value['std']; } ?>' />	
<?php

break;
case 'container_open':
?>
<div id="<?php if(isset($value['id'])) { echo $value['id']; } ?>" class="<?php echo $value['std']; ?>">
<?php

break;
case 'container_close':
?>
</div>
<?php

break;
case 'sub_heading':
?>
<h4 class="<?php if(isset($value['class'])) { echo $value['class']; } ?>"><?php echo $value['name']; ?></h4>
<span class="sub_desc"><?php if(isset($value['desc'])) { echo $value['desc']; } ?></span>
<?php

break;
case "section":

$i++;

?>

	<div class="section ui-tabs-hide" id="<?php echo str_replace(" ","_", strtolower($value['name'])); ?>">
		<div class="rm_title">
			<h3><?php echo $value['name']; ?></h3>
		</div>
	<div class="rm_options">

<?php break;
	}
}
?>

		</div>
	</div>

	<div class="save_bar_top">
		<span class="formState"><?php _e('Theme options have changed, be sure to save changes before leaving this page &#x2192;', 'framework') ?></span>
	    <input type="submit" class="button-primary t2t-button" name="save<?php echo $i; ?>" value="<?php _e('Save All Changes', 'framework') ?>">
		<input type="hidden" name="action" value="save" />
	</div>

	<div class="t2t_footer">
		<span class="t2t_version"><?php echo $themename; ?> v<?php echo $themeversion; ?></span>
		<span class="t2t_built">Built by <a href="http://themes.two2twelve.com">Two2Twelve</a></span>
		<div style="clear:both;"></div>
	</div>

</form>
</div>

<?php
}
?>