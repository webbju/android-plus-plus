<?php

// Custom Twitter Widget
class T2T_Twitter_Widget extends WP_Widget {
	function T2T_Twitter_Widget() {
		global $themename;
        $widget_ops = array('classname' => $themename.'_twitter_widget', 'description' => __('Display your most recent tweets from Twitter', 'framework'));
		$control_ops = array('width' => 200, 'height' => 200);
		$this->WP_Widget('t2t_twitter', __($themename.' - Twitter'), $widget_ops, $control_ops);
	}

	function widget($args, $instance) {
		global $shortname;
        extract( $args );
		$title = apply_filters('widget_title', empty($instance['title']) ? __('Recent Tweets', 'framework') : $instance['title'], $instance, $this->id_base);
		$id = $instance['id'];
		
		if ( !$number = (int) $instance['number'] )
			$number = 5;
		else if ( $number < 1 )
			$number = 1;
		else if ( $number > 15 )
			$number = 15;
		
		$limit = $number;
        ?>

			<?php echo $before_widget; ?>
				<?php echo $before_title . $title . $after_title; ?>
					<div class="twitter_stream"></div>					
					<script type="text/javascript" charset="utf-8">
						$(document).ready(function() {
							$("#<?php echo $this->id; ?> .twitter_stream").tweet({
					            username: "<?php echo $id; ?>",
					            count: <?php echo $limit; ?>,
								template: "{text}{time}",
								retweets: false,
					            loading_text: "loading tweets..."
					        });
						});
					</script>		
			<?php echo $after_widget; ?>

        <?php
	}

	function update($new_instance, $old_instance) {
		$instance = $old_instance;
		$instance['title'] = strip_tags($new_instance['title']);
		$instance['id'] = strip_tags($new_instance['id']);
		$instance['number'] = (int) $new_instance['number'];
				
        return $instance;
	}

	function form($instance) {
		$title = isset($instance['title']) ? esc_attr($instance['title']) : '';
		$id = isset($instance['id']) ? esc_attr($instance['id']) : '';
		if ( !isset($instance['number']) || !$number = (int) $instance['number'] )
			$number = 5;
        ?>

		<p><label for="<?php echo $this->get_field_id('title'); ?>"><?php _e('Title:', 'framework'); ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('title'); ?>" name="<?php echo $this->get_field_name('title'); ?>" type="text" value="<?php echo $title; ?>" /></p>

		<p><label for="<?php echo $this->get_field_id('id'); ?>"><?php _e('Twitter Username:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('id'); ?>" name="<?php echo $this->get_field_name('id'); ?>" type="text" value="<?php echo $id; ?>" /></p>
			
		<p><label for="<?php echo $this->get_field_id('number'); ?>"><?php _e('Number of Tweets to Display:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('number'); ?>" name="<?php echo $this->get_field_name('number'); ?>" type="text" value="<?php echo $number; ?>" /></p>
		
		<?php
	}
}
register_widget('T2T_Twitter_Widget');


// Custom Flickr Widget
class T2T_Flickr_Widget extends WP_Widget {
	function T2T_Flickr_Widget() {
		global $themename;
        $widget_ops = array('classname' => $themename.'_flickr_widget', 'description' => __('Display your photos from Flickr', 'framework'));
		$control_ops = array('width' => 200, 'height' => 200);
		$this->WP_Widget('t2t_flickr', __($themename.' - Flickr', 'framework'), $widget_ops, $control_ops);
	}

	function widget($args, $instance) {
		global $shortname;
        extract( $args );
		$title = apply_filters('widget_title', empty($instance['title']) ? __('Flickr', 'framework') : $instance['title'], $instance, $this->id_base);
		$id = $instance['id'];
		
		$number = (int) $instance['number'];
		if(!$number) { $number = 6; } 
		else if($number < 1) { $number = 1; }
		else if($number > 15) { $number = 15; }
		$limit = $number;
		
        ?>

			<?php echo $before_widget; ?>
				<?php echo $before_title . $title . $after_title; ?>
				<div class="gallery_wrap">
					<script type="text/javascript" src="http://www.flickr.com/badge_code_v2.gne?count=<?php echo $limit; ?>&amp;display=latest&amp;size=s&amp;layout=x&amp;source=user&amp;user=<?php echo $id; ?>"></script> 
					<div class="clear"></div>
				</div>
			<?php echo $after_widget; ?>

        <?php
	}

	function update($new_instance, $old_instance) {
		$instance = $old_instance;
		$instance['title'] = strip_tags($new_instance['title']);
		$instance['id'] = strip_tags($new_instance['id']);
		$instance['number'] = (int) $new_instance['number'];
				
        return $instance;
	}

	function form($instance) {
		$title = isset($instance['title']) ? esc_attr($instance['title']) : '';
		$id = isset($instance['id']) ? esc_attr($instance['id']) : '';
		if ( !isset($instance['number']) || !$number = (int) $instance['number'] )
			$number = 6;
        ?>

		<p><label for="<?php echo $this->get_field_id('title'); ?>"><?php _e('Title:', 'framework'); ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('title'); ?>" name="<?php echo $this->get_field_name('title'); ?>" type="text" value="<?php echo $title; ?>" /></p>

		<p><label for="<?php echo $this->get_field_id('id'); ?>"><?php _e('Flickr ID:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('id'); ?>" name="<?php echo $this->get_field_name('id'); ?>" type="text" value="<?php echo $id; ?>" />
		<span class="hint">Find your Flickr ID <a href="http://idgettr.com/" target="_blank">here</a>.</span>
		</p>
			
		<p><label for="<?php echo $this->get_field_id('number'); ?>"><?php _e('Number of Photos to Display:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('number'); ?>" name="<?php echo $this->get_field_name('number'); ?>" type="text" value="<?php echo $number; ?>" /></p>
		
		<?php
	}
}
register_widget('T2T_Flickr_Widget');


// Custom Reviews Widget
class T2T_Reviews_Widget extends WP_Widget {
	function T2T_Reviews_Widget() {
		global $themename;
        $widget_ops = array('classname' => $themename.'_reviews_widget', 'description' => __('Add rotating reviews to your sidebar', 'framework'));
		$control_ops = array('width' => 400, 'height' => 200);
		$this->WP_Widget('t2t_reviews', __($themename.' - Reviews', 'framework'), $widget_ops, $control_ops);
	}

	function widget($args, $instance) {
		global $shortname;
        extract( $args );
		$title = apply_filters('widget_title', empty($instance['title']) ? __('Reviews', 'framework') : $instance['title'], $instance, $this->id_base);
		$reviews = $instance['reviews'];
		
        ?>

			<?php echo $before_widget; ?>
				<?php echo $before_title . $title . $after_title; ?>
				<div id="reviews">
					<?php $r_count = 1; ?>
					<?php foreach ($reviews as $key => $value) { ?>					
						<div id="review-<?php echo $r_count; ?>" class="review">
							<blockquote><?php echo $value; ?></blockquote>
						</div>		
						<?php $r_count += 1; ?>		
					<?php } ?>
				
					<?php $n_count = 1; ?>
					<ul>
					<?php foreach ($reviews as $key => $value) { ?>					
						<li><a href="#review-<?php echo $n_count; ?>"><span></span></a></li>
						<?php $n_count += 1; ?>		
					<?php } ?>
					</ul>
				</div>
			<?php echo $after_widget; ?>

        <?php
	}

	function update($new_instance, $old_instance) {
		$instance = $old_instance;
		$instance['title'] = strip_tags($new_instance['title']);
		$instance['reviews'] = $new_instance['review'];
				
        return $instance;
	}

	function form($instance) {
		$title = isset($instance['title']) ? esc_attr($instance['title']) : '';
		$reviews = isset($instance['reviews']) ? $instance['reviews'] : '';
        ?>

		<p><label for="<?php echo $this->get_field_id('title'); ?>"><?php _e('Title:', 'framework'); ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('title'); ?>" name="<?php echo $this->get_field_name('title'); ?>" type="text" value="<?php echo $title; ?>" /></p>
		<div id="t2t_review_fields_<?php echo $this->id; ?>">
			<a href="javascript:;" class="add_review button" rel="<?php echo $this->get_field_name("review"); ?>" style="display: block; width: 85px; margin-bottom: 10px;">+ Add a review</a>
			<?php if($reviews != "") { ?>
				<?php $count = 0; ?>
				<?php foreach ($reviews as $key => $value) { ?>			
					<p><label for="<?php echo $this->get_field_name("review"); ?>[]"><?php _e('Review', 'framework') ?> (<a href="javascript:;" class="remove_review file-error">delete</a>):</label><textarea class="widefat" name="<?php echo $this->get_field_name("review"); ?>[]" ><?php echo $value; ?></textarea></p>
					<?php $count += 1; ?>
				<?php } ?>
			<?php } ?>
		</div>	
		
		<?php
	}
}
register_widget('T2T_Reviews_Widget');


// Custom Location Widget
class T2T_Location_Widget extends WP_Widget {
	function T2T_Location_Widget() {
		global $themename;
        $widget_ops = array('classname' => $themename.'_location_widget', 'description' => __('Display your location information', 'framework'));
		$control_ops = array('width' => 200, 'height' => 200);
		$this->WP_Widget('t2t_location', __($themename.' - Location', 'framework'), $widget_ops, $control_ops);
	}

	function widget($args, $instance) {
		global $shortname;
        extract( $args );
		$title = apply_filters('widget_title', empty($instance['title']) ? __('Location', 'framework') : $instance['title'], $instance, $this->id_base);
		$address = $instance['address'];
		$email = $instance['email'];
		$phone = $instance['phone'];
		$fax = $instance['fax'];
		$map_url = $instance['map_url'];
		
        ?>

			<?php echo $before_widget; ?>
				<div class="location">
					<?php echo $before_title . $title . $after_title; ?>
					<?php if($address != "") { ?><p class="address"><?php echo $address; ?></p><?php } ?>
					<?php if($phone != "") { ?><p><strong><?php _e('P:', 'framework') ?></strong> <?php echo $phone; ?></p><?php } ?>
					<?php if($fax != "") { ?><p><strong><?php _e('F:', 'framework') ?></strong> <?php echo $fax; ?></p><?php } ?>
					<?php if($email != "") { ?><p><strong><?php _e('E:', 'framework') ?></strong> <a href="mailto:<?php echo $email; ?>"><?php echo $email; ?></a></p><?php } ?>
					<?php if($map_url != "") { ?>
					<div class="map">
						<iframe width="208" height="90" frameborder="0" scrolling="no" marginheight="0" 
						marginwidth="0" src="<?php echo $map_url; ?>&amp;output=embed"></iframe>
					</div>
					<?php } ?>
				</div>
			<?php echo $after_widget; ?>

        <?php
	}

	function update($new_instance, $old_instance) {
		$instance = $old_instance;
		$instance['title'] = strip_tags($new_instance['title']);
		$instance['address'] = $new_instance['address'];
		$instance['email'] = strip_tags($new_instance['email']);
		$instance['phone'] = strip_tags($new_instance['phone']);
		$instance['fax'] = strip_tags($new_instance['fax']);
		$instance['map_url'] = strip_tags($new_instance['map_url']);
				
        return $instance;
	}

	function form($instance) {
		$title = isset($instance['title']) ? esc_attr($instance['title']) : 'Location';
		$address = isset($instance['address']) ? esc_attr($instance['address']) : '';
		$email = isset($instance['email']) ? esc_attr($instance['email']) : '';
		$phone = isset($instance['phone']) ? esc_attr($instance['phone']) : '';
		$fax = isset($instance['fax']) ? esc_attr($instance['fax']) : '';
		$map_url = isset($instance['map_url']) ? esc_attr($instance['map_url']) : 'http://maps.google.com/maps?q=Google+Inc,+Amphitheatre+Parkway,+Mountain+View,+CA&hl=en&sll=37.0625,-95.677068&sspn=57.030354,107.050781&vpsrc=0&t=m&z=15';
        ?>

		<p><label for="<?php echo $this->get_field_id('title'); ?>"><?php _e('Title:', 'framework'); ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('title'); ?>" name="<?php echo $this->get_field_name('title'); ?>" type="text" value="<?php echo $title; ?>" /></p>

		<p><label for="<?php echo $this->get_field_id('address'); ?>"><?php _e('Address:', 'framework') ?></label>
		<textarea class="widefat" id="<?php echo $this->get_field_id('address'); ?>" name="<?php echo $this->get_field_name('address'); ?>"><?php echo $address; ?></textarea></p>
		
		<p><label for="<?php echo $this->get_field_id('email'); ?>"><?php _e('Email:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('email'); ?>" name="<?php echo $this->get_field_name('email'); ?>" type="text" value="<?php echo $email; ?>" /></p>
		
		<p><label for="<?php echo $this->get_field_id('phone'); ?>"><?php _e('Phone:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('phone'); ?>" name="<?php echo $this->get_field_name('phone'); ?>" type="text" value="<?php echo $phone; ?>" /></p>
		
		<p><label for="<?php echo $this->get_field_id('fax'); ?>"><?php _e('Fax:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('fax'); ?>" name="<?php echo $this->get_field_name('fax'); ?>" type="text" value="<?php echo $fax; ?>" /></p>
			
		<p><label for="<?php echo $this->get_field_id('map_url'); ?>"><?php _e('Map URL:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('map_url'); ?>" name="<?php echo $this->get_field_name('map_url'); ?>" type="text" value="<?php echo $map_url; ?>" /></p>
		
		<?php
	}
}
register_widget('T2T_Location_Widget');


// Custom Video Widget
class T2T_Video_Widget extends WP_Widget {
	function T2T_Video_Widget() {
		global $themename;
        $widget_ops = array('classname' => $themename.'_video_widget', 'description' => __('Display a YouTube or Vimeo video', 'framework'));
		$control_ops = array('width' => 200, 'height' => 200);
		$this->WP_Widget('t2t_video', __($themename.' - Video', 'framework'), $widget_ops, $control_ops);
	}

	function widget($args, $instance) {
		global $shortname;
        extract( $args );
		$title = apply_filters('widget_title', empty($instance['title']) ? __('Video', 'framework') : $instance['title'], $instance, $this->id_base);
		$desc = $instance['desc'];
		$embed = $instance['embed'];
		
        ?>

			<?php echo $before_widget; ?>
				<div class="video">
					<?php echo $before_title . $title . $after_title; ?>
					<?php echo $embed ?>
					<p><?php echo $desc ?></p>
				</div>
			<?php echo $after_widget; ?>

        <?php
	}

	function update($new_instance, $old_instance) {
		$instance = $old_instance;
		$instance['title'] = strip_tags($new_instance['title']);
		$instance['desc'] = stripslashes($new_instance['desc']);
		$instance['embed'] = stripslashes($new_instance['embed']);
				
        return $instance;
	}

	function form($instance) {
		$title = isset($instance['title']) ? esc_attr($instance['title']) : '';
		$desc = isset($instance['desc']) ? esc_attr($instance['desc']) : '';
		$embed = isset($instance['embed']) ? esc_attr($instance['embed']) : '';
        ?>

		<p><label for="<?php echo $this->get_field_id('title'); ?>"><?php _e('Title:', 'framework'); ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('title'); ?>" name="<?php echo $this->get_field_name('title'); ?>" type="text" value="<?php echo $title; ?>" /></p>

		<p><label for="<?php echo $this->get_field_id('embed'); ?>"><?php _e('Embed Code:', 'framework') ?></label>
		<textarea class="widefat" id="<?php echo $this->get_field_id('embed'); ?>" name="<?php echo $this->get_field_name('embed'); ?>"><?php echo $embed; ?></textarea></p>

		<p><label for="<?php echo $this->get_field_id('desc'); ?>"><?php _e('Short Description:', 'framework') ?></label>
		<textarea class="widefat" id="<?php echo $this->get_field_id('desc'); ?>" name="<?php echo $this->get_field_name('desc'); ?>"><?php echo $desc; ?></textarea></p>
		
		<?php
	}
}
register_widget('T2T_Video_Widget');


// Custom Gallery Widget
class T2T_Gallery_Widget extends WP_Widget {
	function T2T_Gallery_Widget() {
		global $themename;
        $widget_ops = array('classname' => $themename.'_gallery_widget', 'description' => __('Display images from your gallery', 'framework'));
		$control_ops = array('width' => 200, 'height' => 200);
		$this->WP_Widget('t2t_gallery', __($themename.' - Gallery', 'framework'), $widget_ops, $control_ops);
	}

	function widget($args, $instance) {
		global $shortname;
        extract( $args );
		$title = apply_filters('widget_title', empty($instance['title']) ? __('Gallery', 'framework') : $instance['title'], $instance, $this->id_base);
		$album = $instance['album'];
		$lightbox = $instance['lightbox'];
		$number = (int) $instance['number'];
		if(!$number) { $number = 6; } 
		else if($number < 1) { $number = 1; }
		else if($number > 30) { $number = 30; }
		$limit = $number;
		
        ?>

			<?php echo $before_widget; ?>
				<?php echo $before_title . $title . $after_title; ?>
				<div class="gallery_wrap">
					<?php 
						if($album != "-1") {
							$taxonomy_slug = get_term_by('id', $album, 'gallery_albums')->slug;
							$taxonomy = '&taxonomy=gallery_albums&term='.$taxonomy_slug;
						} else { $taxonomy = ""; }
					
						if($lightbox == true) {
							$disable_lightbox = false;
						} else {
							$disable_lightbox = true;
						}
					
						$query = new WP_Query();
						$query->query('post_type=t2t_gallery'.$taxonomy.'&posts_per_page='.$limit);  
					 	while ($query->have_posts()) : $query->the_post();  
					?>  
					<div class="image">
						<?php if ( (function_exists('has_post_thumbnail')) && (has_post_thumbnail()) ) : ?>
							
							<?php t2t_lightbox(get_the_ID(), 'post-thumb', $disable_lightbox); ?>
							
						<?php endif; ?>
					</div>
					<?php endwhile; wp_reset_query(); ?>	
					
					<div class="clear"></div>
				</div>
			<?php echo $after_widget; ?>

        <?php
	}

	function update($new_instance, $old_instance) {
		$instance = $old_instance;
		$instance['title'] = strip_tags($new_instance['title']);
		$instance['album'] = stripslashes($new_instance['album']);
		$instance['lightbox'] = stripslashes($new_instance['lightbox']);
		$instance['number'] = stripslashes($new_instance['number']);
				
        return $instance;
	}

	function form($instance) {
		$title = isset($instance['title']) ? esc_attr($instance['title']) : '';
		$album = isset($instance['album']) ? esc_attr($instance['album']) : '';
		$lightbox = isset($instance['lightbox']) ? esc_attr($instance['lightbox']) : '';
		if ( !isset($instance['number']) || !$number = (int) $instance['number'] )
			$number = 6;
        ?>

		<p><label for="<?php echo $this->get_field_id('title'); ?>"><?php _e('Title:', 'framework'); ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('title'); ?>" name="<?php echo $this->get_field_name('title'); ?>" type="text" value="<?php echo $title; ?>" /></p>

		<p><label for="<?php echo $this->get_field_id('album'); ?>"><?php _e('Album:', 'framework') ?></label><br/>
			<?php 
				wp_dropdown_categories(array(
					'hide_empty' => 0, 
					'show_option_none' => 'All Images',
					'name' => $this->get_field_name('album'),
					'selected' => $album,
					'taxonomy' => 'gallery_albums'
				)); 
			?>
		</p>

		<p><label for="<?php echo $this->get_field_id('number'); ?>"><?php _e('Number:', 'framework') ?></label>
		<input class="widefat" id="<?php echo $this->get_field_id('number'); ?>" name="<?php echo $this->get_field_name('number'); ?>" type="text" value="<?php echo $number; ?>" /></p>
		
		<?php if($lightbox == "true"){ $checked = "checked=\"checked\""; }else{ $checked = "";} ?>
		<p><label for="<?php echo $this->get_field_id('lightbox'); ?>"><?php _e('Lightbox:', 'framework') ?></label>
		<input type="checkbox" class="checkbox" name="<?php echo $this->get_field_name('lightbox'); ?>" id="<?php echo $this->get_field_id('lightbox'); ?>" value="true" <?php echo $checked; ?> /><br/>
		<span class="hint">Check this to open images in lightbox. If not, images will link to their respective pages.</span>
		</p>
		
		<?php
	}
}
register_widget('T2T_Gallery_Widget');

?>