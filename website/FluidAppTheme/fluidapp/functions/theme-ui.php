<?php

/*-----------------------------------------------------------------------------------*/
/*	Function to output logo for "logo" theme option
/*-----------------------------------------------------------------------------------*/
function t2t_logo() {
	if(get_option('t2t_logo_type') == "text"  && get_option('t2t_logo_text') != "") { 
		$out = '<div class="text">';
	} else if(get_option('t2t_logo_type') == "upload"  && get_option('t2t_logo') != "") {
		$out = '<div class="uploaded">';
	} else {
		$out = '<div class="logo">';
	}
	
	$home = home_url();
	
	if(get_option('t2t_logo_type') == "upload" && get_option('t2t_logo') != "") {
		$out .= '<a href="'.$home.'"><img src="'.get_option('t2t_logo').'" alt="'.get_option('blogname').'" /></a>';
	} else if(get_option('t2t_logo_type') == "text"  && get_option('t2t_logo_text') != "") {
		$out .= '<a href="'.$home.'">'.get_option('t2t_logo_text').'</a>';
	} else {
		$out .= '<a href="'.$home.'"><img src="'.get_template_directory_uri().'/images/'.strtolower(get_option('t2t_theme_color')).'-logo.png" alt="'.get_option('blogname').'" /></a>';
	}
	$out .= '</div>';
	
	echo $out;
}

/*-----------------------------------------------------------------------------------*/
/*	Function to output app store buttons
/*-----------------------------------------------------------------------------------*/
function t2t_app_store_buttons() {
	
	$button_list = array();
	if(get_option('t2t_button_apple')) { $button_list['apple'] = array(__('iPhone', 'framework'), get_option('t2t_button_apple')); }
	if(get_option('t2t_button_android')) { $button_list['android'] = array(__('Android', 'framework'), get_option('t2t_button_android')); }
	if(get_option('t2t_button_blackberry')) { $button_list['blackberry'] = array(__('Blackberry', 'framework'), get_option('t2t_button_blackberry')); }
	if(get_option('t2t_button_windows')) { $button_list['windows'] = array(__('Windows', 'framework'), get_option('t2t_button_windows')); }
	
	$out = "";
	foreach ($button_list as $button => $url) {
				
		$out .= '<a href="'.$url[1].'" class="large_button" id="'.$button.'">';
		$out .= '<span class="icon"></span>';
		if(get_option('t2t_button_'.strtolower($url[0]).'_text') != "") {
			$out .= get_option('t2t_button_'.strtolower($url[0]).'_text');
		} else {
			$out .= '<small>'.__('Download now for', 'framework').'</small> '.$url[0].'';
		}
		$out .= '</a>';
		
	}
	
	echo $out;
}

/*-----------------------------------------------------------------------------------*/
/*	Function to output social links
/*-----------------------------------------------------------------------------------*/
function t2t_social_links() {

	$social_links = array();
	if(get_option('t2t_social_twitter')) 				{  $social_links['twitter']   	  = array('&#62218;', get_option('t2t_social_twitter')); }
	if(get_option('t2t_social_facebook')) 			{  $social_links['facebook']      = array('&#62221;', get_option('t2t_social_facebook')); }
	if(get_option('t2t_social_flickr')) 				{  $social_links['flickr'] 	  	  = array('&#62212;', get_option('t2t_social_flickr')); }
	if(get_option('t2t_social_github')) 				{  $social_links['github'] 	  	  = array('&#62209;', get_option('t2t_social_github')); }
	if(get_option('t2t_social_vimeo')) 					{  $social_links['vimeo'] 	  	  = array('&#62215;', get_option('t2t_social_vimeo')); }
	if(get_option('t2t_social_google_plus')) 		{  $social_links['google_plus'] 	= array('&#62224;', get_option('t2t_social_google_plus')); }
	if(get_option('t2t_social_pinterest')) 			{  $social_links['pinterest'] 		= array('&#62227;', get_option('t2t_social_pinterest')); }
	if(get_option('t2t_social_linkedin')) 			{  $social_links['linkedin'] 			= array('&#62233;', get_option('t2t_social_linkedin')); }
	if(get_option('t2t_social_dribbble')) 			{  $social_links['dribbble'] 			= array('&#62236;', get_option('t2t_social_dribbble')); }
	if(get_option('t2t_social_stumbleupon')) 		{  $social_links['stumbleupon'] 	= array('&#62239;', get_option('t2t_social_stumbleupon')); }
	if(get_option('t2t_social_lastfm')) 				{  $social_links['lastfm'] 				= array('&#62242;', get_option('t2t_social_lastfm')); }
	if(get_option('t2t_social_rdio')) 					{  $social_links['rdio'] 					= array('&#62245;', get_option('t2t_social_rdio')); }
	if(get_option('t2t_social_spotify')) 				{  $social_links['spotify'] 			= array('&#62248;', get_option('t2t_social_spotify')); }
	if(get_option('t2t_social_skype')) 					{  $social_links['skype'] 				= array('&#62266;', get_option('t2t_social_skype')); }
	if(get_option('t2t_social_info')) 					{  $social_links['info'] 			  	= array('&#59141;', get_option('t2t_social_info')); }

	$out = '<ul class="social">';
	foreach ($social_links as $site => $url) {
		$out .= '<li><a href="'.$url[1].'" title="'.$site.'"><span class="social_icon">'.$url[0].'</span></a></li>';
	}
	$out .= '</ul>';
	
	echo $out;
}

/*-----------------------------------------------------------------------------------*/
/*	Custom comment formatting
/*-----------------------------------------------------------------------------------*/
function t2t_comment($comment, $args, $depth) {
	$GLOBALS['comment'] = $comment;
	switch ( $comment->comment_type ) :
		case '' :
	?>
	<li id="comment-<?php comment_ID(); ?>">
		<?php echo get_avatar($comment, 50); ?>
		<div class="comment">
			<h5><?php printf(sprintf( '%s', get_comment_author_link())); ?></h5>
			<span class="date"><?php echo get_comment_date(); ?> at <?php echo get_comment_time(); ?></span>
			<p><?php comment_text(); ?></p>
			<?php if ($comment->comment_approved == '0') : ?>
				<br />
				<p><em><?php _e( 'Your comment is awaiting moderation.', 'framework'); ?></em></p>
			<?php endif; ?>
			<?php comment_reply_link(array_merge( $args, array('reply_text' => 'Reply', 'depth' => $depth, 'max_depth' => $args['max_depth']))); ?>
		</div>
<?php
			break;
	endswitch;
}

/*-----------------------------------------------------------------------------------*/
/*	Retrieve a formatted slide element for given a slide
/*-----------------------------------------------------------------------------------*/
function t2t_slide_device($device) {
	if($device == "iPhone 5 (black)") { $out = "iphone-five-black"; }
	if($device == "iPhone 5 (white)") { $out = "iphone-five-white"; }
	if($device == "iPhone 4S (black)") { $out = "iphone-black"; }
	if($device == "iPhone 4S (white)") { $out = "iphone-white"; }
	if($device == "Blackberry") { $out = "blackberry"; }
	if($device == "Android") { $out = "android"; }
	if($device == "Windows") { $out = "windows"; }	
	if($device == "iPad (white)") { $out = "ipad-white"; }
	if($device == "iPad (black)") { $out = "ipad-black"; }
	
	return $out;
}

function t2t_slide_video($video) {
	if(preg_match('/youtube/', $video)) {
		
		if(preg_match('/[\\?\\&]v=([^\\?\\&]+)/', $video, $matches)) {
			$output = '<iframe title="YouTube video player" class="youtube-player" type="text/html" width="230" height="345" src="http://www.youtube.com/embed/'.$matches[1].'?rel=0&controls=0&showinfo=0" frameborder="0" allowFullScreen></iframe>';
		} else {
			$output = __('Sorry that seems to be an invalid <strong>YouTube</strong> URL. Please double check it.', 'framework');
		}
		
	} elseif(preg_match('/vimeo/', $video))  {
		
		if(preg_match('~^http://(?:www\.)?vimeo\.com/(?:clip:)?(\d+)~', $video, $matches)) {
			$output = '<iframe src="http://player.vimeo.com/video/'.$matches[1].'?title=0&amp;byline=0&amp;portrait=0" width="230" height="345" frameborder="0"></iframe>';
		} else  {
			$output = __('Sorry that seems to be an invalid <strong>Vimeo</strong> URL. Please double check it. Make sure there is a string of numbers at the end.', 'framework');
		}
		
	} else  {
		$output = __('Sorry that is an invalid YouTube or Vimeo URL.', 'framework');
	}
	return $output;
}

function t2t_slide($slide) {
	
	if(get_option($slide.'_background_screenshot') || get_option($slide.'_background_video_url') || get_option($slide.'_foreground_screenshot') || get_option($slide.'_foreground_video_url')) {
	
		$out = '<div class="slide">';
		
		if(get_option($slide.'_background_screenshot') || get_option($slide.'_background_video_url')) {
			$out .= '<div class="background '.t2t_slide_device(get_option($slide.'_background_device')).'">';
			if(!get_option($slide.'_background_video_url')) {
				$out .= '<img src="'.get_option($slide.'_background_screenshot').'" alt="" />';
			} else {
				$out .= t2t_slide_video(get_option($slide.'_background_video_url'));
			}
			$out .= '</div>';
		}
		
		if(get_option($slide.'_background_device') != "iPad (white)" && get_option($slide.'_background_device') != "iPad (black)") {
		
			if(get_option($slide.'_foreground_screenshot') || get_option($slide.'_foreground_video_url')) {
				$out .= '<div class="foreground '.t2t_slide_device(get_option($slide.'_foreground_device')).'">';
				if(!get_option($slide.'_foreground_video_url')) {
					$out .= '<img src="'.get_option($slide.'_foreground_screenshot').'" alt="" />';
				} else {
					$out .= t2t_slide_video(get_option($slide.'_foreground_video_url'));
				}
				$out .= '</div>';
			}
		}
	
		$out .= '</div>';
	
		echo $out;
	
	}
}

/*-----------------------------------------------------------------------------------*/
/*	Function to output site menu
/*-----------------------------------------------------------------------------------*/
function t2t_menu() {
	
	//If WP 3.0 or > include support for wp_nav_menu()
	if(t2t_check_wp_version()){
		
		if ( has_nav_menu( 'primary-menu' ) ) {
			wp_nav_menu( array( 'theme_location' => 'primary-menu', 'container_class' => '', 'container' => false, 'menu_id' => '', 'fallback_cb' => '' ) );
			return;
		}
	}
	
	$home = home_url();

	$active_class = (is_front_page()) ? 'class="current-menu-item"' : '';
	$out = '';
	$out .= '<ul>';
	$out .= wp_list_pages("sort_column=menu_order&title_li=&echo=0&depth=0");
	$out .= '</ul>';
	
	echo $out;
}

function t2t_truncate($str, $length=10, $trailing='...') {
	  $length-=mb_strlen($trailing);
	  if (mb_strlen($str)> $length) {
		 return mb_substr($str,0,$length).$trailing;
	  }
	  else {
		 $res = $str;
	  }
	  return $res;
}

function is_multiple($number, $multiple) { 
   return ($number % $multiple) == 0; 
}

function t2t_lightbox($post_id, $size, $echo=true) {
	
	$thumb = get_the_post_thumbnail($post_id, $size);
	$original = wp_get_attachment_image_src(get_post_thumbnail_id($post_id), 'original');
	$video = get_post_meta($post_id, '_video_url', true);
	$embed = get_post_meta($post_id, '_embed_code', true);
	$height = get_post_meta($post_id, '_height', true);
	
	if($video != "" || $embed != "") {
		$li_class = "video";
	} else {
		$li_class = "";
	}

	if($height == '')
		$height = 391;
	
	if($embed != '') {
		$output = '<a href="'.get_template_directory_uri().'/functions/includes/gallery-video.php?id='.$post_id.'&iframe=true&width=710&height='.$height.'" title="'.get_the_title($post_id).'" class="fancybox" rel="gallery">'.$thumb.'</a>';
	} elseif($video != '' && $embed == '') {
		if (strpos($video, 'youtube') > 0) {
	        $link_class = 'youtube';
					$video_id = preg_match('/[\\?\\&]v=([^\\?\\&]+)/', $video, $matches);
					$thumb = '<img src="http://img.youtube.com/vi/'.$matches[1].'/default.jpg" />';
	    } elseif (strpos($video, 'vimeo') > 0) {
					
					$video_id = preg_match('~^http://(?:www\.)?vimeo\.com/(?:clip:)?(\d+)~', $video, $matches);
					$hash = unserialize(file_get_contents('http://vimeo.com/api/v2/video/'.$matches[1].'.php'));
					$thumb = '<img src="'.$hash[0]['thumbnail_small'].'" />';
	        $link_class = 'vimeo';
	    }
		$output = '<a href="'.$video.'" title="'.get_the_title($post_id).'" class="'.$link_class.'" rel="gallery">'.$thumb.'</a>';
	} else {
		$output = '<a href="'.$original[0].'" title="'.get_the_title($post_id).'" class="fancybox" rel="gallery">'.$thumb.'</a>';
	}
	
	if($echo == true) {
		echo $output;
	} else {
		return $output;
	}
	
}

function t2t_feature_class($post) {
		
	if(get_the_post_thumbnail($post, 'feature-icon')) {
		$class = "with_image";
	} elseif(get_post_meta($post, '_icon', true)) {
		$class = "with_icon";
	} else {
		$class = "none";
	}
	echo $class;
}


?>