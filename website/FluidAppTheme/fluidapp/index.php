<?php get_header(); ?>

<!-- Start Blog -->
<div id="blog" class="page">
	
	<h1><?php _e('Blog', 'framework'); ?></h1>
	
	<?php if (have_posts()) : while (have_posts()) : the_post(); ?>
	<div id="post_id_<?php the_ID(); ?>" <?php post_class('post'); ?>> 
	
		<h2>
			<a href="<?php the_permalink(); ?>"><?php the_title(); ?></a>
		</h2>

		<div class="meta">
			<span class="author">
				<span class="icon">&#128100;</span>
				<?php the_author_posts_link(); ?>
			</span>
			<span class="date">
				<span class="icon">&#128340;</span>
				<?php the_time('F j, Y') ?>
			</span>
			<span class="comments">
				<span class="icon">&#59168;</span>
				<?php comments_popup_link('0', '1', '%'); ?>
			</span>
		</div>
		
		<?php
		  if (has_post_thumbnail()) {
				$image_id = get_post_thumbnail_id();  
				$image_url = wp_get_attachment_image_src($image_id,'post-thumbnail');  
				$image_url = $image_url[0];
				echo '<img src="'.$image_url.'" alt="" class="rounded" />';
			}
		?>
		
		<div class="content">
			<?php the_content(); ?>
		</div>
		
		<!-- Start Comments -->
    <div id="comments">
      <?php comments_template('', true); ?>
    </div>
    <!-- End Comments -->
		
	</div>
	<?php endwhile; ?>
		<div class="pagination">
			<a href=""><?php next_posts_link('&#x2190; Previous') ?></a>
			<a href=""><?php previous_posts_link('Next &#x2192;') ?></a>
		</div>
  <?php else : ?>
		<p>
		  <?php _e('Sorry, no posts matched your criteria.', 'framework') ?>
		</p>
	<?php endif; ?>
	
</div>
<!-- End Blog -->

<?php get_footer(); ?>