# import rsa
import base64
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.hazmat.backends import default_backend

e = 65537
n_bits = 2048

key = rsa.generate_private_key(e, n_bits, default_backend())
default_padding = padding.PKCS1v15


def encrypt(text, pbk):
    return pbk.encrypt(text, default_padding())


def encryptb64(textb64, pbk):
    text_enc = encrypt(textb64, pbk)
    return base64.b64encode(text_enc)


def decrypt(text_bytes, prk):
    return prk.decrypt(text_bytes, default_padding())


def decryptb64(textb64, prk):
    text_bytes = base64.b64decode(textb64)
    return decrypt(text_bytes, prk)


# def encrypt(text_decoded, e, n):
#     pbk = rsa.RSAPublicNumbers(e, n).public_key(default_backend())
#     return pbk.encrypt(text_decoded, default_padding())


def generate_new_private_key():
    return rsa.generate_private_key(e, n_bits, default_backend())


def create_public_key(e, n):
    return rsa.RSAPublicNumbers(e, n).public_key(default_backend())
