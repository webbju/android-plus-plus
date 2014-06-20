<?php get_header(); ?>
<div class="page">
		
<?php if (have_posts()) : while (have_posts()) : the_post(); ?>
	
	<div class="feature_list content_box">
		<div class="feature_heading">				
			<h3 class="<?php t2t_feature_class($post->ID); ?>">
				<?php 
					if(get_the_post_thumbnail($post->ID, 'feature-icon')) {
						echo get_the_post_thumbnail($post->ID, 'feature-icon');
					} else if(get_post_meta($post->ID, '_icon', true)) {
						
				?>
					<span class="icon" style="color: <?php echo get_post_meta($post->ID, '_icon_color', true); ?>;"><?php echo get_post_meta($post->ID, '_icon', true); ?></span> 
				<?php } ?>
				<?php echo get_post_meta('_read_more', $post->ID, true);?>
				<?php the_title(); ?>
			</h3>
		</div>
		<div class="content">
			<?php the_content(); ?>
		</div>
	</div>
	
	<!-- Start Comments -->
  <div id="comments">
    <?php comments_template('', true); ?>
  </div>
  <!-- End Comments -->
	
	<?php endwhile; endif; ?>
	
</div>
<?php get_footer(); ?>