   M S B A M L   `   `   `  ����  GridSandbox    GridSandbox.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  Grid Sandbox��5   	   $	��720��5   	   $
��1800��6    � 	Resources 5         MediumGreyBrush �� Color  KinectPurpleBrush  KinectBlueBrush��%�   &	        &	 #     &	 F     �� 5	   
   $ 	#ff6e6e6e��62   �� 5
   
   $ 	#ff52318f��64   �� 5   
   $ 	#ff00BCF2��62   5      .��5      � -   5         �Name$ UIGrid��5      $��0 0 0 0q�6    � RowDefinitions 5   
   �� 5      $	��600 �6   �� 5      $��40 �6   �� 5      $��40 �6    � ColumnDefinitions 5   
   �� 5      $	��600 �6   �� 5      $	��600 �6   �� 5      $	��600 �6   .+�5   
   (� $��0��6   $��0��6   $��Center=�6/   .��5      �� $��UniformToFill=�63   ��6   �� *ImageSource+63   5      �� 5   
   -   5        $��	statusBar��5   ?   $��1��6   $��Stretch=�6!   $��Bottom=�6P   
�������#��[  6~   .@�5      �� ��6   �� *
StatusText+5      A� 5   
   -   5         A�Name$ uniGrid��5      $��0��6'   $��1��64   6F   A� 5    
   -   5        $ 
uniImgGrid��5       $��0��6*   $��1��67   6I   �� 5"   
   -   5         ��Name$ 
canvasOver��5"      $��0��6%   $��1��62   .+�5#      �� -   5         ��Name$
 Hand��5#      	 �� Left$	 0��6"   

 �� Top$
 0��62   $��1��6A   $��Images\openhand.png��6Q   $
��Fill=�6n   $
��Left=�6}   $	��Top=�6�   $��50��6�   $��50��6�   �� 5$      -   5        $ Hand2��5$      $	 0��6#   $
 0��63   $��1��6B   $��Images\openhand.png��6R   $
��Fill=�6o   $
��Left=�6~   $	��Top=�6�   $��50��6�   $��50��6�   $y�Hidden=�6�   5%      �� 5&   
   -	   5         ��Name$ textHere��5&      $
��Left=�6&   $��1��6A   $
��Wrap=�6N   $	��Top=�6b   $��46��6z   $	��592��6�   $��1��6�   $��36(�6�   $��0,0,0,-5q�6�   $��
Bauhaus 93)�6�   $��	Invasion!��6�   �� 5'   
   -
   5        $ instructionText��5'      $
��Left=�6-   $��0��6H   $��2��6U   $
��Wrap=�6e   $	��Top=�6y   $��Set Instruction Text Here��6�   $��24(�6�   $	��590��6�   $��
Bauhaus 93)�6�   �� 5(   
   -   5         ��Name$ Button1��5(      $��Button1��6"   $��2��64   $
��Left=�6D   $��	5,574,0,0q�6_   $	��Top=�6r   $	��144��6�   $	��105��6�   $��20(�6�   $��3��6�   �� 5)   
   -   5        $ Button2��5)      $��Button2��6"   $��2��64   $
��Left=�6D   $��225,574,0,0q�6_   $	��Top=�6t   $	��144��6�   $	��105��6�   $��20(�6�   $��3��6�   �� 5*   
   -   5        $ Button3��5*      $��Button3��6"   $��2��64   $
��Left=�6D   $��438,574,0,0q�6_   $	��Top=�6t   $	��144��6�   $	��105��6�   $��20(�6�   $��3��6�   5+      5,      