import cv2 as cv
import pandas as pd
import numpy as np


res_x = 4096
res_y = 4096

pd_read = pd.read_csv("Assets/scripts/noise_generation/generated_noise_texture.csv", sep=',', header=None)
noise_texture = pd_read.values.reshape(res_x, res_y, 3)

noise_texture = noise_texture.astype('uint8')
cv.imwrite("Assets/scripts/noise_generation/generated_noise_texture.png", noise_texture)

