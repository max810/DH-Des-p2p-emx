# import rsa
import base64
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.hazmat.backends import default_backend

e = 65537
n_bits = 2048

key = rsa.generate_private_key(e, n_bits, default_backend())
default_padding = padding.PKCS1v15()

# cipher = base64.b64encode(key.encrypt('Hello World. This is a test using Python3'.encode(), padding.PKCS1v15()))
# print(cipher)


#
# __public_key, __private_key = rsa.newkeys(512, accurate=True)
#
# print("-----PUBLIC KEY-----", __public_key, "-----PRIVATE KEY-----", __private_key, sep='\n')
#
#
def decrypt(text_encoded):
    return key.decrypt(text_encoded, default_padding)


def encrypt(text_decoded, e, n):
    pbk = rsa.RSAPublicNumbers(e, n).public_key(default_backend())
    return pbk.encrypt(text_decoded, default_padding)


def generate_new_keys():
    global key
    key = rsa.generate_private_key(e, n_bits, default_backend())


def get_public_numbers():
    return key.public_key().public_numbers()
