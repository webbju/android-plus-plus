<?php

/*-----------------------------------------------------------------------------------*/
/*	Define Custom Post Options
/*-----------------------------------------------------------------------------------*/
function t2t_meta_boxes($meta_name = false) {
	global $themename;
	
	$adminurl = admin_url('admin.php?page=functions.php');
	
	$meta_boxes = array(
		
		't2t_screenshots' => array(
			'id' => 't2t_screenshots_meta',
			'title' => $themename . ' Screenshot Options',
			'function' => 't2t_screenshots_meta_box',
			'noncename' => 't2t_screenshots',
			'type' => 't2t_screenshots',
			'fields' => array(
				'desc' => array(
					'name' => 'desc',
					'type' => 'desc',
					'width' => '',
					'default' => '',
					'description' => 'To upload an image, use the "Set featured image" option. For a video, use the options below.',
					'label' => '',
					'margin' => true,
				),
				'video_url' => array(
					'name' => '_video_url',
					'type' => 'text',
					'width' => '',
					'default' => '',
					'title' => 'Youtube or Vimeo URL',
					'description' => "If you'd like to use a YouTube or Vimeo video, enter in the page URL here.",
					'label' => '',
					'margin' => true,
				),
				'embed_code' => array(
					'name' => '_embed_code',
					'type' => 'textarea',
					'width' => '',
					'default' => '',
					'title' => 'Embed Code',
					'description' => "If you'd like to use something other than YouTube or Vimeo, copy/paste the embed code here. This field will override the above.",
					'label' => '',
					'margin' => true,
				), 
				'height' => array(
					'name' => '_height',
					'type' => 'text',
					'width' => '',
					'default' => '',
					'title' => 'Video height',
					'description' => "Enter the height you'd like to use for this video. <b>Note:</b> 500 = (500px).",
					'label' => '',
					'margin' => true,
				),
		  )
		),
		
		't2t_press' => array(
			'id' => 't2t_press_meta',
			'title' => $themename . ' Press Options',
			'function' => 't2t_press_meta_box',
			'noncename' => 't2t_press',
			'type' => 't2t_press',
			'fields' => array(
				'Author' => array(
					'name' => '_author',
					'type' => 'text',
					'width' => '',
					'default' => '',
					'title' => 'Author',
					'description' => 'The name of the author for this press mention.',
					'label' => '',
					'margin' => true,
				),
				'Website' => array(
					'name' => '_website',
					'type' => 'text',
					'width' => '',
					'default' => '',
					'title' => 'Website',
					'description' => 'The originating website of this press mention.',
					'label' => '',
					'margin' => true,
				)
		  )
		),
		
		't2t_updates' => array(
			'id' => 't2t_updates_meta',
			'title' => $themename . ' Update Options',
			'function' => 't2t_updates_meta_box',
			'noncename' => 't2t_updates',
			'type' => 't2t_updates',
			'fields' => array(
				'read_more' => array(
					'name' => '_read_more',
					'type' => 'checkbox',
					'width' => '',
					'default' => '',
					'title' => 'Enable Title Link',
					'description' => "Check this box to enable the title linking to this update's full page.",
					'label' => '',
					'margin' => true,
				),
				'Release Date' => array(
					'name' => '_release_date',
					'type' => 'text',
					'width' => '',
					'default' => '',
					'title' => 'Release Date',
					'description' => 'The date this update was released',
					'label' => '',
					'margin' => true,
				)
		  )
		),
		
		't2t_features' => array(
			'id' => 't2t_features_meta',
			'title' => $themename . ' Feature Options',
			'function' => 't2t_features_meta_box',
			'noncename' => 't2t_features',
			'type' => 't2t_features',
			'fields' => array(
				'read_more' => array(
					'name' => '_read_more',
					'type' => 'checkbox',
					'width' => '',
					'default' => '',
					'title' => 'Enable Title Link',
					'description' => "Check this box to enable the title linking to this feature's full details page.",
					'label' => '',
					'margin' => true,
				),
				'Icon' => array(
					'name' => '_icon',
					'type' => 'icon_select',
					'width' => '',
					'default' => '',
					'title' => 'Icon',
					'description' => 'Select the icon to be displayed for this feature.',
					'label' => '',
					'margin' => true,
				),
				'Icon Color' => array(
					'name' => '_icon_color',
					'type' => 'colorpicker',
					'width' => '',
					'default' => '#333333',
					'title' => 'Icon Color',
					'description' => 'Select the color you\'d like to use for this icon',
					'label' => '',
					'margin' => true,
				)
		  )
		),
		
	);
	if ($meta_name)
		return $meta_boxes[$meta_name];
	else
		return $meta_boxes;
}

/*-----------------------------------------------------------------------------------*/
/*	Add meta boxes
/*-----------------------------------------------------------------------------------*/

// Each custom post type w/ options needs a function like this!
function t2t_screenshots_meta_box() {
	t2t_add_meta_box('t2t_screenshots');
}
function t2t_press_meta_box() {
	t2t_add_meta_box('t2t_press');
}
function t2t_updates_meta_box() {
	t2t_add_meta_box('t2t_updates');
}
function t2t_features_meta_box() {
	t2t_add_meta_box('t2t_features');
}

function t2t_add_meta_boxes() {
	$meta_boxes = t2t_meta_boxes();
	
	foreach ($meta_boxes as $meta_box) {	
		add_meta_box($meta_box['id'], $meta_box['title'], $meta_box['function'], $meta_box['type'], 'normal', 'high');	
	}

}
add_action('admin_menu', 't2t_add_meta_boxes');

/*-----------------------------------------------------------------------------------*/
/*	Build options UI
/*-----------------------------------------------------------------------------------*/
function t2t_add_meta_box($box_name) {
	global $post, $themename;
	$meta_box = t2t_meta_boxes($box_name);

	foreach ($meta_box['fields'] as $meta_id => $meta_field){
	
			$existing_value = get_post_meta($post->ID, $meta_field['name'], true);
			$value = ($existing_value != '') ? $existing_value : $meta_field['default'];
			$margin = ($meta_field['margin']) ? ' class="add_margin"' : '';

			if ($meta_field['description']) {
				$description = '<p class="description">' . $meta_field['description'] . '</p>' . "\n";
			}else{
				$switch = '';
				$description = '';
			}
	
?>
	<div id="<?php echo $meta_id; ?>" class="<?php echo $themename; ?>-post-control t2t-form t2t-post-options">


	<?php switch ( $meta_field['type'] ) { 

	case "select_sidebar":
	?>
	<p<?php echo $margin; ?>>
	<select name="<?php echo $meta_field['name']; ?>">
		<option value=""<?php if($existing_value == ''){ echo " selected";} ?>>Select A Sidebar</option>
	<?php
	$sidebars = sidebar_generator_t2t::get_sidebars();
	if(is_array($sidebars) && !empty($sidebars)){
		foreach($sidebars as $sidebar){
			if($existing_value == $sidebar){
				echo "<option value='$sidebar' selected>$sidebar</option>\n";
			}else{
				echo "<option value='$sidebar'>$sidebar</option>\n";
			}
		}
	}
?>
	</select>
	</p><br/>
<?php
break;
case "text":
?>
	
	<div class="row">
		<label><?php echo $meta_field['title']; ?></label>
		<div class="content">
			<input type="text" name="<?php echo $meta_field['name']; ?>" value="<?php echo $existing_value; ?>"  class="t2t_field" />
			<span class="hint"><?php if ($description){echo $description;}?></span>
		</div>
	</div>
	
<?php
break;
case "textarea":
?>

	<div class="row">
		<label><?php echo $meta_field['title']; ?></label>
		<div class="content">
			<textarea name="<?php echo $meta_field['name']; ?>" class="t2t_field"><?php echo $existing_value; ?></textarea>
			<span class="hint"><?php if ($description){echo $description;}?></span>
		</div>
	</div>

<?php
break;
case "checkbox":
?>

	<div class="row">
		<label><?php echo $meta_field['title']; ?></label>
		<div class="content check">
			<input type="checkbox" name="<?php echo $meta_field['name']; ?>" value="1" <?php if($existing_value == 1){ echo "checked='checked'"; } ?> />
			<span class="hint"><?php if ($description){echo $description;}?></span>
		</div>
	</div>
	
<?php
break;
case "hidden":
?>

	<div class="row">
		<input type="hidden" name="<?php echo $meta_field['name']; ?>" value="<?php echo $meta_field['default']; ?>" />
	</div>

<?php
break;
case "desc":
?>

	<div class="row">
		<p><?php echo $meta_field['description']; ?></p>
	</div>

<?php
break;
case "colorpicker":
?>

	<div class="row">
		<label><?php echo $meta_field['title']; ?></label>
		<div class="content">
			<div id="<?php echo $meta_field['name']; ?>_picker" class="colorSelector"><div style="background-color: <?php if ( $existing_value != "") { echo stripslashes($existing_value); } else { echo $meta_field['default']; } ?>;"></div></div>
			
			<input name="<?php echo $meta_field['name']; ?>" id="<?php echo $value['id']; ?>" type="text" value='<?php if ( $existing_value != "") { echo stripslashes($existing_value); } else { echo $meta_field['default']; } ?>' style="width: 80px;" />
			<br/>
		 	<span class="hint"><?php if ($description){echo $description;}?></span>
		</div>
	</div>

	
<?php
break;
case "icon_select":
?>

	<div class="row">
		<label><?php echo $meta_field['title']; ?></label>
		<div class="content check icon_select">				
				<script type="text/javascript" charset="utf-8">
					jQuery(document).ready(function($) {
						$("ul.icons li").each(function() {
							var title = $(this).attr("title");
							$(this).append("<span class='desc'>"+title+"</span>");
														
							if($(".icon", this).html() != "") {							
								$(".icon", this).attr("data-entity", "&"+$(".icon", this).text());
								$(".icon", this).html("&"+$(".icon", this).text());
							}
							
							$(this).click(function() {

								this_elem = $(this);
								
								$("ul.icons li").removeClass("selected");
								this_elem.addClass("selected");
								$(".selected_icon .icon").html($(".icon", this_elem).text());
								$(".selected_icon .desc").html($(".desc", this_elem).text());
								$(".icon_field").val($(this).find(".icon").attr("data-entity"));
								
							});
						});
						
						<?php if($existing_value != ''){ ?>
							$("ul.icons li .icon:contains('<?php echo $existing_value; ?>')").parent().addClass("selected")
							$(".selected_icon .desc").text($("ul.icons li .icon[data-entity='<?php echo $existing_value; ?>']").parent().attr("title"));
						<?php } ?>
						$("ul.icons").fadeIn("fast");
					});
				</script>
				
				<b>SELECTED ICON</b>
				<div class="selected_icon">
					<span class="icon"><?php echo $existing_value; ?></span>
					<span class="desc"><?php if($existing_value == ''){ ?>No icon<?php } ?></span>
				</div>
				
				<input type="hidden" class="icon_field" name="<?php echo $meta_field['name']; ?>" value="<?php echo htmlentities($existing_value); ?>" />
				
				<b>AVAILABLE ICONS <span>(click an icon to select it)</span></b>
				<ul class="icons" style="display: none;">
					<li title="No icon">
						<span class="icon"></span>
					</li>	
					<li title="phone">
						<span class="icon">#128222;</span>
					</li>
					<li title="mobile">
						<span class="icon">#128241;</span>
					</li>
					<li title="mouse">
						<span class="icon">#59273;</span>
					</li>
					<li title="address">
						<span class="icon">#59171;</span>
					</li>
					<li title="mail">
						<span class="icon">#9993;</span>
					</li>
					<li title="paper-plane">
						<span class="icon">#128319;</span>
					</li>
					<li title="pencil">
						<span class="icon">#9998;</span>
					</li>
					<li title="feather">
						<span class="icon">#10002;</span>
					</li>
					<li title="attach">
						<span class="icon">#128206;</span>
					</li>
					<li title="inbox">
						<span class="icon">#59255;</span>
					</li>
					<li title="reply">
						<span class="icon">#59154;</span>
					</li>
					<li title="reply-all">
						<span class="icon">#59155;</span>
					</li>
					<li title="forward">
						<span class="icon">#10150;</span>
					</li>
					<li title="user">
						<span class="icon">#128100;</span>
					</li>
					<li title="users">
						<span class="icon">#128101;</span>
					</li>
					<li title="add-user">
						<span class="icon">#59136;</span>
					</li>
					<li title="vcard">
						<span class="icon">#59170;</span>
					</li>
					<li title="export">
						<span class="icon">#59157;</span>
					</li>
					<li title="location">
						<span class="icon">#59172;</span>
					</li>
					<li title="map">
						<span class="icon">#59175;</span>
					</li>
					<li title="compass">
						<span class="icon">#59176;</span>
					</li>
					<li title="direction">
						<span class="icon">#10146;</span>
					</li>
					<li title="hair-cross">
						<span class="icon">#127919;</span>
					</li>
					<li title="share">
						<span class="icon">#59196;</span>
					</li>
					<li title="shareable">
						<span class="icon">#59198;</span>
					</li>
					<li title="heart">
						<span class="icon">hearts;</span>
					</li>
					<li title="heart-empty">
						<span class="icon">#9825;</span>
					</li>
					<li title="star">
						<span class="icon">#9733;</span>
					</li>
					<li title="star-empty">
						<span class="icon">#9734;</span>
					</li>
					<li title="thumbs-up">
						<span class="icon">#128077;</span>
					</li>
					<li title="thumbs-down">
						<span class="icon">#128078;</span>
					</li>
					<li title="chat">
						<span class="icon">#59168;</span>
					</li>
					<li title="comment">
						<span class="icon">#59160;</span>
					</li>
					<li title="quote">
						<span class="icon">#10078;</span>
					</li>
					<li title="home">
						<span class="icon">#8962;</span>
					</li>
					<li title="popup">
						<span class="icon">#59212;</span>
					</li>
					<li title="search">
						<span class="icon">#128269;</span>
					</li>
					<li title="flashlight">
						<span class="icon">#128294;</span>
					</li>
					<li title="print">
						<span class="icon">#59158;</span>
					</li>
					<li title="bell">
						<span class="icon">#128276;</span>
					</li>
					<li title="link">
						<span class="icon">#128279;</span>
					</li>
					<li title="flag">
						<span class="icon">#9873;</span>
					</li>
					<li title="cog">
						<span class="icon">#9881;</span>
					</li>
					<li title="tools">
						<span class="icon">#9874;</span>
					</li>
					<li title="trophy">
						<span class="icon">#127942;</span>
					</li>
					<li title="tag">
						<span class="icon">#59148;</span>
					</li>
					<li title="camera">
						<span class="icon">#128247;</span>
					</li>
					<li title="megaphone">
						<span class="icon">#128227;</span>
					</li>
					<li title="moon">
						<span class="icon">#9789;</span>
					</li>
					<li title="palette">
						<span class="icon">#127912;</span>
					</li>
					<li title="leaf">
						<span class="icon">#127810;</span>
					</li>
					<li title="note">
						<span class="icon">#9834;</span>
					</li>
					<li title="beamed-note">
						<span class="icon">#9835;</span>
					</li>
					<li title="new">
						<span class="icon">#128165;</span>
					</li>
					<li title="graduation-cap">
						<span class="icon">#127891;</span>
					</li>
					<li title="book">
						<span class="icon">#128213;</span>
					</li>
					<li title="newspaper">
						<span class="icon">#128240;</span>
					</li>
					<li title="bag">
						<span class="icon">#128092;</span>
					</li>
					<li title="airplane">
						<span class="icon">#9992;</span>
					</li>
					<li title="lifebuoy">
						<span class="icon">#59272;</span>
					</li>
					<li title="eye">
						<span class="icon">#59146;</span>
					</li>
					<li title="clock">
						<span class="icon">#128340;</span>
					</li>
					<li title="mic">
						<span class="icon">#127908;</span>
					</li>
					<li title="calendar">
						<span class="icon">#128197;</span>
					</li>
					<li title="flash">
						<span class="icon">#9889;</span>
					</li>
					<li title="thunder-cloud">
						<span class="icon">#9928;</span>
					</li>
					<li title="droplet">
						<span class="icon">#128167;</span>
					</li>
					<li title="cd">
						<span class="icon">#128191;</span>
					</li>
					<li title="briefcase">
						<span class="icon">#128188;</span>
					</li>
					<li title="air">
						<span class="icon">#128168;</span>
					</li>
					<li title="hourglass">
						<span class="icon">#9203;</span>
					</li>
					<li title="gauge">
						<span class="icon">#128711;</span>
					</li>
					<li title="language">
						<span class="icon">#127892;</span>
					</li>
					<li title="network">
						<span class="icon">#59254;</span>
					</li>
					<li title="key">
						<span class="icon">#128273;</span>
					</li>
					<li title="battery">
						<span class="icon">#128267;</span>
					</li>
					<li title="bucket">
						<span class="icon">#128254;</span>
					</li>
					<li title="magnet">
						<span class="icon">#59297;</span>
					</li>
					<li title="drive">
						<span class="icon">#128253;</span>
					</li>
					<li title="cup">
						<span class="icon">#9749;</span>
					</li>
					<li title="rocket">
						<span class="icon">#128640;</span>
					</li>
					<li title="brush">
						<span class="icon">#59290;</span>
					</li>
					<li title="suitcase">
						<span class="icon">#128710;</span>
					</li>
					<li title="traffic-cone">
						<span class="icon">#128712;</span>
					</li>
					<li title="globe">
						<span class="icon">#127758;</span>
					</li>
					<li title="keyboard">
						<span class="icon">#9000;</span>
					</li>
					<li title="browser">
						<span class="icon">#59214;</span>
					</li>
					<li title="publish">
						<span class="icon">#59213;</span>
					</li>
					<li title="progress-3">
						<span class="icon">#59243;</span>
					</li>
					<li title="progress-2">
						<span class="icon">#59242;</span>
					</li>
					<li title="progress-1">
						<span class="icon">#59241;</span>
					</li>
					<li title="progress-0">
						<span class="icon">#59240;</span>
					</li>
					<li title="light-down">
						<span class="icon">#128261;</span>
					</li>
					<li title="light-up">
						<span class="icon">#128262;</span>
					</li>
					<li title="adjust">
						<span class="icon">#9681;</span>
					</li>
					<li title="code">
						<span class="icon">#59156;</span>
					</li>
					<li title="monitor">
						<span class="icon">#128187;</span>
					</li>
					<li title="infinity">
						<span class="icon">infin;</span>
					</li>
					<li title="light-bulb">
						<span class="icon">#128161;</span>
					</li>
					<li title="credit-card">
						<span class="icon">#128179;</span>
					</li>
					<li title="database">
						<span class="icon">#128248;</span>
					</li>
					<li title="voicemail">
						<span class="icon">#9991;</span>
					</li>
					<li title="clipboard">
						<span class="icon">#128203;</span>
					</li>
					<li title="cart">
						<span class="icon">#59197;</span>
					</li>
					<li title="box">
						<span class="icon">#128230;</span>
					</li>
					<li title="ticket">
						<span class="icon">#127915;</span>
					</li>
					<li title="rss">
						<span class="icon">#59194;</span>
					</li>
					<li title="signal">
						<span class="icon">#128246;</span>
					</li>
					<li title="thermometer">
						<span class="icon">#128255;</span>
					</li>
					<li title="water">
						<span class="icon">#128166;</span>
					</li>
					<li title="sweden">
						<span class="icon">#62977;</span>
					</li>
					<li title="line-graph">
						<span class="icon">#128200;</span>
					</li>
					<li title="pie-chart">
						<span class="icon">#9716;</span>
					</li>
					<li title="bar-graph">
						<span class="icon">#128202;</span>
					</li>
					<li title="area-graph">
						<span class="icon">#128318;</span>
					</li>
					<li title="lock">
						<span class="icon">#128274;</span>
					</li>
					<li title="lock-open">
						<span class="icon">#128275;</span>
					</li>
					<li title="logout">
						<span class="icon">#59201;</span>
					</li>
					<li title="login">
						<span class="icon">#59200;</span>
					</li>
					<li title="check">
						<span class="icon">#10003;</span>
					</li>
					<li title="cross">
						<span class="icon">#10060;</span>
					</li>
					<li title="squared-minus">
						<span class="icon">#8863;</span>
					</li>
					<li title="squared-plus">
						<span class="icon">#8862;</span>
					</li>
					<li title="squared-cross">
						<span class="icon">#10062;</span>
					</li>
					<li title="circled-minus">
						<span class="icon">#8854;</span>
					</li>
					<li title="circled-plus">
						<span class="icon">oplus;</span>
					</li>
					<li title="circled-cross">
						<span class="icon">#10006;</span>
					</li>
					<li title="minus">
						<span class="icon">#10134;</span>
					</li>
					<li title="plus">
						<span class="icon">#10133;</span>
					</li>
					<li title="erase">
						<span class="icon">#9003;</span>
					</li>
					<li title="block">
						<span class="icon">#128683;</span>
					</li>
					<li title="info">
						<span class="icon">#8505;</span>
					</li>
					<li title="circled-info">
						<span class="icon">#59141;</span>
					</li>
					<li title="help">
						<span class="icon">#10067;</span>
					</li>
					<li title="circled-help">
						<span class="icon">#59140;</span>
					</li>
					<li title="warning">
						<span class="icon">#9888;</span>
					</li>
					<li title="cycle">
						<span class="icon">#128260;</span>
					</li>
					<li title="cw">
						<span class="icon">#10227;</span>
					</li>
					<li title="ccw">
						<span class="icon">#10226;</span>
					</li>
					<li title="shuffle">
						<span class="icon">#128256;</span>
					</li>
					<li title="back">
						<span class="icon">#128281;</span>
					</li>
					<li title="level-down">
						<span class="icon">#8627;</span>
					</li>
					<li title="retweet">
						<span class="icon">#59159;</span>
					</li>
					<li title="loop">
						<span class="icon">#128257;</span>
					</li>
					<li title="back-in-time">
						<span class="icon">#59249;</span>
					</li>
					<li title="level-up">
						<span class="icon">#8624;</span>
					</li>
					<li title="switch">
						<span class="icon">#8646;</span>
					</li>
					<li title="numbered-list">
						<span class="icon">#57349;</span>
					</li>
					<li title="add-to-list">
						<span class="icon">#57347;</span>
					</li>
					<li title="layout">
						<span class="icon">#9871;</span>
					</li>
					<li title="list">
						<span class="icon">#9776;</span>
					</li>
					<li title="text-doc">
						<span class="icon">#128196;</span>
					</li>
					<li title="text-doc-inverted">
						<span class="icon">#59185;</span>
					</li>
					<li title="doc">
						<span class="icon">#59184;</span>
					</li>
					<li title="docs">
						<span class="icon">#59190;</span>
					</li>
					<li title="landscape-doc">
						<span class="icon">#59191;</span>
					</li>
					<li title="picture">
						<span class="icon">#127748;</span>
					</li>
					<li title="video">
						<span class="icon">#127916;</span>
					</li>
					<li title="music">
						<span class="icon">#127925;</span>
					</li>
					<li title="folder">
						<span class="icon">#128193;</span>
					</li>
					<li title="archive">
						<span class="icon">#59392;</span>
					</li>
					<li title="trash">
						<span class="icon">#59177;</span>
					</li>
					<li title="upload">
						<span class="icon">#128228;</span>
					</li>
					<li title="download">
						<span class="icon">#128229;</span>
					</li>
					<li title="save">
						<span class="icon">#128190;</span>
					</li>
					<li title="install">
						<span class="icon">#59256;</span>
					</li>
					<li title="cloud">
						<span class="icon">#9729;</span>
					</li>
					<li title="upload-cloud">
						<span class="icon">#59153;</span>
					</li>
					<li title="bookmark">
						<span class="icon">#128278;</span>
					</li>
					<li title="bookmarks">
						<span class="icon">#128209;</span>
					</li>
					<li title="open-book">
						<span class="icon">#128214;</span>
					</li>
					<li title="play">
						<span class="icon">#9654;</span>
					</li>
					<li title="paus">
						<span class="icon">#8214;</span>
					</li>
					<li title="record">
						<span class="icon">#9679;</span>
					</li>
					<li title="stop">
						<span class="icon">#9632;</span>
					</li>
					<li title="ff">
						<span class="icon">#9193;</span>
					</li>
					<li title="fb">
						<span class="icon">#9194;</span>
					</li>
					<li title="to-start">
						<span class="icon">#9198;</span>
					</li>
					<li title="to-end">
						<span class="icon">#9197;</span>
					</li>
					<li title="resize-full">
						<span class="icon">#59204;</span>
					</li>
					<li title="resize-small">
						<span class="icon">#59206;</span>
					</li>
					<li title="volume">
						<span class="icon">#9207;</span>
					</li>
					<li title="sound">
						<span class="icon">#128266;</span>
					</li>
					<li title="mute">
						<span class="icon">#128263;</span>
					</li>
					<li title="flow-cascade">
						<span class="icon">#128360;</span>
					</li>
					<li title="flow-branch">
						<span class="icon">#128361;</span>
					</li>
					<li title="flow-tree">
						<span class="icon">#128362;</span>
					</li>
					<li title="flow-line">
						<span class="icon">#128363;</span>
					</li>
					<li title="flow-parallel">
						<span class="icon">#128364;</span>
					</li>
					<li title="left-bold">
						<span class="icon">#58541;</span>
					</li>
					<li title="down-bold">
						<span class="icon">#58544;</span>
					</li>
					<li title="up-bold">
						<span class="icon">#58543;</span>
					</li>
					<li title="right-bold">
						<span class="icon">#58542;</span>
					</li>
					<li title="left">
						<span class="icon">#11013;</span>
					</li>
					<li title="down">
						<span class="icon">#11015;</span>
					</li>
					<li title="up">
						<span class="icon">#11014;</span>
					</li>
					<li title="right">
						<span class="icon">#10145;</span>
					</li>
					<li title="circled-left">
						<span class="icon">#59225;</span>
					</li>
					<li title="circled-down">
						<span class="icon">#59224;</span>
					</li>
					<li title="circled-up">
						<span class="icon">#59227;</span>
					</li>
					<li title="circled-right">
						<span class="icon">#59226;</span>
					</li>
					<li title="triangle-left">
						<span class="icon">#9666;</span>
					</li>
					<li title="triangle-down">
						<span class="icon">#9662;</span>
					</li>
					<li title="triangle-up">
						<span class="icon">#9652;</span>
					</li>
					<li title="triangle-right">
						<span class="icon">#9656;</span>
					</li>
					<li title="chevron-left">
						<span class="icon">#59229;</span>
					</li>
					<li title="chevron-down">
						<span class="icon">#59228;</span>
					</li>
					<li title="chevron-up">
						<span class="icon">#59231;</span>
					</li>
					<li title="chevron-right">
						<span class="icon">#59230;</span>
					</li>
					<li title="chevron-small-left">
						<span class="icon">#59233;</span>
					</li>
					<li title="chevron-small-down">
						<span class="icon">#59232;</span>
					</li>
					<li title="chevron-small-up">
						<span class="icon">#59235;</span>
					</li>
					<li title="chevron-small-right">
						<span class="icon">#59234;</span>
					</li>
					<li title="chevron-thin-left">
						<span class="icon">#59237;</span>
					</li>
					<li title="chevron-thin-down">
						<span class="icon">#59236;</span>
					</li>
					<li title="chevron-thin-up">
						<span class="icon">#59239;</span>
					</li>
					<li title="chevron-thin-right">
						<span class="icon">#59238;</span>
					</li>
					<li title="left-thin">
						<span class="icon">larr;</span>
					</li>
					<li title="down-thin">
						<span class="icon">darr;</span>
					</li>
					<li title="up-thin">
						<span class="icon">uarr;</span>
					</li>
					<li title="right-thin">
						<span class="icon">rarr;</span>
					</li>
					<li title="arrow-combo">
						<span class="icon">#59215;</span>
					</li>
					<li title="three-dots">
						<span class="icon">#9206;</span>
					</li>
					<li title="two-dots">
						<span class="icon">#9205;</span>
					</li>
					<li title="dot">
						<span class="icon">#9204;</span>
					</li>
					<li title="cc">
						<span class="icon">#128325;</span>
					</li>
					<li title="cc-by">
						<span class="icon">#128326;</span>
					</li>
					<li title="cc-nc">
						<span class="icon">#128327;</span>
					</li>
					<li title="cc-nc-eu">
						<span class="icon">#128328;</span>
					</li>
					<li title="cc-nc-jp">
						<span class="icon">#128329;</span>
					</li>
					<li title="cc-sa">
						<span class="icon">#128330;</span>
					</li>
					<li title="cc-nd">
						<span class="icon">#128331;</span>
					</li>
					<li title="cc-pd">
						<span class="icon">#128332;</span>
					</li>
					<li title="cc-zero">
						<span class="icon">#128333;</span>
					</li>
					<li title="cc-share">
						<span class="icon">#128334;</span>
					</li>
					<li title="cc-remix">
						<span class="icon">#128335;</span>
					</li>
					<li title="db-logo">
						<span class="icon">#128505;</span>
					</li>
					<li title="db-shape">
						<span class="icon">#128506;</span>
					</li>
					<li title="github">
						<span class="icon">#62208;</span>
					</li>
					<li title="c-github">
						<span class="icon">#62209;</span>
					</li>
					<li title="flickr">
						<span class="icon">#62211;</span>
					</li>
					<li title="c-flickr">
						<span class="icon">#62212;</span>
					</li>
					<li title="vimeo">
						<span class="icon">#62214;</span>
					</li>
					<li title="c-vimeo">
						<span class="icon">#62215;</span>
					</li>
					<li title="twitter">
						<span class="icon">#62217;</span>
					</li>
					<li title="c-twitter">
						<span class="icon">#62218;</span>
					</li>
					<li title="facebook">
						<span class="icon">#62220;</span>
					</li>
					<li title="c-facebook" class="last">
						<span class="icon">#62221;</span>
					</li>
					<li title="s-facebook">
						<span class="icon">#62222;</span>
					</li>
					<li title="google+">
						<span class="icon">#62223;</span>
					</li>
					<li title="c-google+">
						<span class="icon">#62224;</span>
					</li>
					<li title="pinterest">
						<span class="icon">#62226;</span>
					</li>
					<li title="c-pinterest">
						<span class="icon">#62227;</span>
					</li>
					<li title="tumblr">
						<span class="icon">#62229;</span>
					</li>
					<li title="c-tumblr">
						<span class="icon">#62230;</span>
					</li>
					<li title="linkedin">
						<span class="icon">#62232;</span>
					</li>
					<li title="c-linkedin">
						<span class="icon">#62233;</span>
					</li>
					<li title="dribbble" class="last">
						<span class="icon">#62235;</span>
					</li>
					<li title="c-dribbble">
						<span class="icon">#62236;</span>
					</li>
					<li title="stumbleupon">
						<span class="icon">#62238;</span>
					</li>
					<li title="c-stumbleupon">
						<span class="icon">#62239;</span>
					</li>
					<li title="lastfm">
						<span class="icon">#62241;</span>
					</li>
					<li title="c-lastfm">
						<span class="icon">#62242;</span>
					</li>
					<li title="rdio">
						<span class="icon">#62244;</span>
					</li>
					<li title="c-rdio">
						<span class="icon">#62245;</span>
					</li>
					<li title="spotify">
						<span class="icon">#62247;</span>
					</li>
					<li title="c-spotify">
						<span class="icon">#62248;</span>
					</li>
					<li title="qq" class="last">
						<span class="icon">#62250;</span>
					</li>
					<li title="instagram">
						<span class="icon">#62253;</span>
					</li>
					<li title="dropbox">
						<span class="icon">#62256;</span>
					</li>
					<li title="evernote">
						<span class="icon">#62259;</span>
					</li>
					<li title="flattr">
						<span class="icon">#62262;</span>
					</li>
					<li title="skype">
						<span class="icon">#62265;</span>
					</li>
					<li title="c-skype">
						<span class="icon">#62266;</span>
					</li>
					<li title="renren">
						<span class="icon">#62268;</span>
					</li>
					<li title="sina-weibo">
						<span class="icon">#62271;</span>
					</li>
					<li title="paypal">
						<span class="icon">#62274;</span>
					</li>
					<li title="picasa" class="last">
						<span class="icon">#62277;</span>
					</li>
					<li title="soundcloud">
						<span class="icon">#62280;</span>
					</li>
					<li title="mixi">
						<span class="icon">#62283;</span>
					</li>
					<li title="behance">
						<span class="icon">#62286;</span>
					</li>
					<li title="google-circles">
						<span class="icon">#62289;</span>
					</li>
					<li title="vk">
						<span class="icon">#62292;</span>
					</li>
					<li title="smashing">
						<span class="icon">#62295;</span>
					</li>
				</ul>
				
			<span class="hint"><?php if ($description){echo $description;}?></span>
		</div>
	</div>
	
<?php
break;
}
?>
	</div>
<?php } ?>
	<input type="hidden" value="<?php echo wp_create_nonce(plugin_basename(__FILE__)); ?>" id="<?php echo $meta_box['noncename']; ?>_noncename" name="<?php echo $meta_box['noncename']; ?>_noncename"/>
<?php 
}

/*-----------------------------------------------------------------------------------*/
/*	Save options
/*-----------------------------------------------------------------------------------*/
function t2t_save_meta($post_id) {
	$meta_boxes = t2t_meta_boxes();
	
	if (isset($_POST['post_type']) && $_POST['post_type'] == 'page') {
		if (!current_user_can('edit_page', $post_id))
			return $post_id;
	}
	else {
		if (!current_user_can('edit_post', $post_id))
			return $post_id;
	}
		if ( isset($_GET['post']) && isset($_GET['bulk_edit']) )
			return $post_id;

	foreach ($meta_boxes as $meta_box) {
		foreach ($meta_box['fields'] as $meta_field) {
			$current_data = get_post_meta($post_id, $meta_field['name'], true);	
			if(isset($_POST[$meta_field['name']])) {
				$new_data = $_POST[$meta_field['name']];
			}

			if ($current_data) {
				if ($new_data == '')
					delete_post_meta($post_id, $meta_field['name']);
				elseif ($new_data == $meta_field['default'])
					delete_post_meta($post_id, $meta_field['name']);
				elseif ($new_data != $current_data)
					update_post_meta($post_id, $meta_field['name'], $new_data);
			}
			elseif (isset($new_data) && $new_data != '')
				add_post_meta($post_id, $meta_field['name'], sanitize_text_field($new_data), true);
		}
	}
}
add_action('save_post', 't2t_save_meta');

?>