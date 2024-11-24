from __future__ import print_function
import os
import cv2
import tqdm
import json
import argparse
import numpy as np
from PIL import Image
from imageio import imwrite
import torch
from torch.utils import data
from torch.utils.data import DataLoader
from torchvision import transforms
import Utils

parser = argparse.ArgumentParser(description='BiFuse script for 360 depth prediction!',
        formatter_class=argparse.ArgumentDefaultsHelpFormatter)
parser.add_argument('--path', default='./My_Test_Data', type=str, help='Path of source images')
parser.add_argument('--nocrop', action='store_true', help='Disable cropping')
args = parser.parse_args()

class MyData(data.Dataset):
    def __init__(self, root):
        imgs = os.listdir(root)
        self.imgs = [os.path.join(root, k) for k in imgs]
        self.transforms = transforms.Compose([
            transforms.ToTensor()
            ])

    def __getitem__(self, index):
        img_path = self.imgs[index]
        rgb_img = Image.open(img_path).convert("RGB")
        rgb_img = np.array(rgb_img, np.float32) / 255
        rgb_img = cv2.resize(rgb_img, (1024, 512), interpolation=cv2.INTER_AREA)
        data = self.transforms(rgb_img)

        return data

    def __len__(self):
        return len(self.imgs)

def Run(loader, model):
    model = model.eval()  

    with torch.no_grad():
        for it, data in enumerate(loader):
            
            inputs = data.cuda()
            raw_pred_var, pred_cube_var, refine = model(inputs)
            
            if it==0:
                break
            

def main():
    test_img = MyData("C:/Users/yusuke/Downloads/BiFuse-master/BiFuse-master/My_Test_Data")
    print('Test Data Num:', len(test_img))
    dataset_val = DataLoader(
            test_img,
            batch_size=1,
            num_workers=2,
            drop_last=False,
            pin_memory=True,
            shuffle=False
            )

    
    from models.FCRN import MyModel as ResNet
    model = ResNet(
    		layers=50,
    		decoder="upproj",
    		output_size=None,
    		in_channels=3,
    		pretrained=True
    		).cuda()

    Run(dataset_val, model)

if __name__ == '__main__':
    main()
